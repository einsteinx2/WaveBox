using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using WaveBox;
using WaveBox.Core.Extensions;
using WaveBox.Transcoding;

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske.

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/
using WaveBox.ApiHandler;
using WaveBox.Static;
using WaveBox.Core.Static;

namespace WaveBox.Service.Services.Http
{
	public class HttpProcessor : IHttpProcessor
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public TcpClient Socket { get; set; }

		private Stream InputStream { get; set; }

		public String HttpMethod { get; set; }
		public String HttpUrl { get; set; }
		public String HttpProtocolVersionString { get; set; }
		public Hashtable HttpHeaders { get; set; }

		public ITranscoder Transcoder { get; set; }

		// Delayed headers, mostly used for updating sessions if needed
		public Dictionary<string, string> DelayedHeaders = new Dictionary<string, string>();

		private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

		public HttpProcessor(TcpClient s)
		{
			HttpHeaders = new Hashtable();
			Socket = s;
		}

		public void Process()
		{
			// we can't use a StreamReader for input, because it buffers up extra data on us inside it's
			// "processed" view of the world, and we want the data raw after the headers
			InputStream = Socket.GetStream();

			// we probably shouldn't be using a streamwriter for all output from handlers either
			try
			{
				InputStream.ReadTimeout = 30000;

				// Read in first line of request, get tokens for HTTP method, URL, version
				this.ParseRequest();

				// Captures hashtable of HTTP headers sent with request
				this.ReadHeaders();

				if (HttpMethod == "GET")
				{
					this.HandleGETRequest();
				}
				else if (HttpMethod == "POST")
				{
					this.HandlePOSTRequest();
				}
				else
				{
					// HTTP 405: Unsupported method
					this.WriteMethodNotAllowedHeader();
				}
			}
			catch (Exception e)
			{
				logger.Error("Exception occurred during HTTP processing");
				logger.Error(e);
				this.WriteErrorHeader();
			}
			finally
			{
				// Ensure all streams and sockets are closed
				InputStream = null;
				Socket.GetStream().Close();
				Socket.Client.Close();
				Socket.Close();
			}
		}

		public void WriteNotModifiedHeader()
		{
			StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
			outStream.WriteLine("HTTP/1.1 304 Not Modified");
			outStream.WriteLine("Connection: close");
			outStream.WriteLine("");
			outStream.Flush();
		}

		public void WriteErrorHeader()
		{
			StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
			outStream.WriteLine("HTTP/1.1 404 Not Found");
			outStream.WriteLine("Connection: close");
			outStream.WriteLine("");
			outStream.Flush();
		}

		public void WriteMethodNotAllowedHeader()
		{
			StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
			outStream.WriteLine("HTTP/1.1 405 Method Not Allowed");
			outStream.WriteLine("Allow: GET, POST");
			outStream.WriteLine("Connection: close");
			outStream.WriteLine("");
			outStream.Flush();
		}

		public void WriteSuccessHeader(long contentLength, string mimeType, IDictionary<string, string> customHeaders, DateTime lastModified, bool isPartial = false, string encoding = null)
		{
			StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
			string status = isPartial ? "HTTP/1.1 206 Partial Content" : "HTTP/1.1 200 OK";
			outStream.WriteLine(status);
			outStream.WriteLine("Date: " + DateTime.UtcNow.ToRFC1123());
			outStream.WriteLine("Server: WaveBox/" + WaveBoxService.BuildVersion);
			outStream.WriteLine("Last-Modified: " + lastModified.ToRFC1123());
			outStream.WriteLine("ETag: \"" + lastModified.ToETag() + "\"");
			outStream.WriteLine("Accept-Ranges: bytes");

			// Check request for compression
			if (encoding != null)
			{
				outStream.WriteLine("Content-Encoding: " + encoding);
			}

			if (contentLength >= 0)
			{
				outStream.WriteLine("Content-Length: " + contentLength);
			}

			outStream.WriteLine("Access-Control-Allow-Origin: *");
			outStream.WriteLine("Content-Type: " + mimeType);

			if ((object)customHeaders != null)
			{
				foreach (string key in customHeaders.Keys)
				{
					outStream.WriteLine(key + ": " + customHeaders[key]);
				}
			}

			// Inject delayed headers
			foreach (string key in DelayedHeaders.Keys)
			{
				outStream.WriteLine(key + ": " + DelayedHeaders[key]);
			}

			outStream.WriteLine("Connection: close");
			outStream.WriteLine("");
			outStream.Flush();

			// Only log API responses
			if (HttpUrl.Contains("api"))
			{
				if (logger.IsInfoEnabled) logger.Info(String.Format("Success, status: {0}, length: {1}, encoding: {2}, ETag: {3}, Last-Modified: {4}",
					status,
					contentLength,
					encoding ?? "none",
					lastModified.ToETag(),
					lastModified.ToRFC1123()
				));
			}
		}

		public void WriteCompressedText(byte[] input, string mimeType, string encoding)
		{
			try
			{
				byte[] output = null;

				// Create a MemoryStream for compression
				using (MemoryStream memStream = new MemoryStream())
				{
					Stream zipStream = null;

					// Attempt GZIP compression
					if (encoding == "gzip")
					{
						zipStream = new GZipStream(memStream, CompressionMode.Compress);
					}
					// Attempt DEFLATE compression
					else if (encoding == "deflate")
					{
						zipStream = new DeflateStream(memStream, CompressionMode.Compress);
					}
					else
					{
						logger.Error("Unknown encoding: " + encoding);
						return;
					}

					// Write compressed data to stream
					zipStream.Write(input, 0, input.Length);
					zipStream.Flush();
					zipStream.Dispose();

					// Grab compressed output from memory
					output = memStream.ToArray();
				}

				// Compression okay, write success header
				WriteSuccessHeader(output.Length, mimeType + ";charset=utf-8", null, DateTime.UtcNow, false, encoding);

				// Write the stream
				var binStream = new BinaryWriter(new BufferedStream(Socket.GetStream()), Encoding.UTF8);
				binStream.Write(output);
				binStream.Flush();
			}
			// If write failure, client disconnected, so ignore and continue
			catch (IOException)
			{
			}
			catch (Exception e)
			{
				logger.Error("Failed to write compressed HTTP response: " + encoding);
				logger.Error(e);
			}

			return;
		}

		public void WriteText(string text, string mimeType)
		{
			// If compression requested, attempt to send compressed
			if (HttpHeaders.ContainsKey("Accept-Encoding"))
			{
				// Check which encoding
				string accepted = HttpHeaders["Accept-Encoding"].ToString();
				string encoding = null;
				if (accepted.Contains("gzip"))
				{
					encoding = "gzip";
				}
				else if (accepted.Contains("deflate"))
				{
					encoding = "deflate";
				}

				// Bad encoding, send plaintext
				if (encoding != null)
				{
					// Send compressed stream if valid encoding
					byte[] input = Encoding.UTF8.GetBytes(text);
					WriteCompressedText(input, mimeType, encoding);
					return;
				}
			}

			// Makes no sense at all, but for whatever reason, all ajax calls fail with a cross site
			// scripting error if Content-Type is set, but the player needs it for files for seeking,
			// so pass -1 for no Content-Length header for all text requests
			WriteSuccessHeader(Encoding.UTF8.GetByteCount(text) + 3, mimeType + ";charset=utf-8", null, DateTime.UtcNow);

			try
			{
				StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()), Encoding.UTF8);
				outStream.Write(text);
				outStream.Flush();
			}
			// If write failure, client disconnected, so ignore and continue
			catch (IOException)
			{
			}
			catch (Exception e)
			{
				logger.Error("Failed to write HTTP response");
				logger.Error(e);
			}
		}

		public void WriteJson(string json)
		{
			WriteText(json, "application/json");
		}

		public void WriteFile(Stream fs, int startOffset, long length, string mimeType, IDictionary<string, string> customHeaders, bool isSendContentLength, DateTime? lastModified, long? limitToBytes = null)
		{
			if ((object)fs == null || !fs.CanRead || length == 0 || startOffset >= length)
			{
				return;
			}

			DateTime lastMod = CleanLastModified(lastModified);

			// If it exists, check to see if the headers contains an If-Modified-Since or If-None-Match entry
			if (HttpHeaders.ContainsKey("If-Modified-Since"))
			{
				if (HttpHeaders["If-Modified-Since"].Equals(lastMod.ToRFC1123()))
				{
					WriteNotModifiedHeader();
					return;
				}
			}
			if (HttpHeaders.ContainsKey("If-None-Match"))
			{
				if (HttpHeaders["If-None-Match"].Equals(lastMod.ToETag()))
				{
					WriteNotModifiedHeader();
					return;
				}
			}

			// Read/Write in 8 KB chunks
			const int chunkSize = 8192;

			// Initialize everything
			byte[] buf = new byte[chunkSize];
			int bytesRead;
			long bytesWritten = 0;
			long totalBytesWritten = 0;
			Socket.SendTimeout = 30000;
			Stream stream = new BufferedStream(Socket.GetStream());
			int sinceLastReport = 0;
			long actualStartOffset = startOffset;
			Stopwatch sw = new Stopwatch();

			if (fs.CanSeek)
			{
				// Seek to the start offset
				fs.Seek(startOffset, SeekOrigin.Begin);
				actualStartOffset = fs.Position;
				if (actualStartOffset < startOffset && !ReferenceEquals(Transcoder, null) && Transcoder.State == TranscodeState.Active)
				{
					// Wait for the file to catch up
					while (Transcoder.State == TranscodeState.Active)
					{
						// Try the seek again
						fs.Seek(startOffset, SeekOrigin.Begin);

						// Check the position
						actualStartOffset = fs.Position;
						if (actualStartOffset >= startOffset)
						{
							// We've made it, so break
							break;
						}

						// Sleep for a bit to prevent a tight loop
						Thread.Sleep(250);
					}
				}

				totalBytesWritten = fs.Position;
			}

			// TODO: make sure content length is correct when doing range requests on transcoded files
			long contentLength = length - actualStartOffset;
			if (!ReferenceEquals(limitToBytes, null) && contentLength > limitToBytes)
			{
				contentLength = (long)limitToBytes;
			}

			bool isPartial = startOffset != 0 || !ReferenceEquals(limitToBytes, null);
			if (isPartial)
			{
				if (ReferenceEquals(customHeaders, null))
				{
					customHeaders = new Dictionary<string, string>();
				}

				string contentRange = "bytes " + startOffset + "-" + (startOffset + contentLength - 1) + "/" + length;
				customHeaders["Content-Range"] = contentRange;
			}

			WriteSuccessHeader(isSendContentLength ? contentLength : -1, mimeType, customHeaders, lastMod, isPartial);
			if (logger.IsInfoEnabled) logger.Info("File header, contentLength: " + contentLength + ", contentType: " + mimeType);

			sw.Start();
			while (true)
			{
				try
				{
					int thisChunkSize = chunkSize;
					if (!ReferenceEquals(limitToBytes, null))
					{
						// Make sure we don't send too much data on the last (potentially) partial chunk
						if (bytesWritten + chunkSize > limitToBytes)
						{
							// Reduce the chunk size
							thisChunkSize = (int)(limitToBytes - bytesWritten);
						}
					}

					// Attempt to read a chunk
					bytesRead = fs.Read(buf, 0, thisChunkSize);

					// Send the bytes out to the client
					stream.Write(buf, 0, bytesRead);
					stream.Flush();
					bytesWritten += bytesRead;
					totalBytesWritten += bytesRead;

					// Log the progress (only for testing)
					if (sw.ElapsedMilliseconds > 1000)
					{
						if (logger.IsInfoEnabled)
						{
							logger.Info(String.Format("[ {0,10} / {1,10} | {2:000}% | {3:00.00000} Mbps ]",
								totalBytesWritten,
								(contentLength + startOffset),
								((Convert.ToDouble(totalBytesWritten) / Convert.ToDouble(contentLength + startOffset)) * 100),
								Math.Round((((double)(sinceLastReport * 8) / 1024) / 1024) / (double)(sw.ElapsedMilliseconds / 1000), 5)
							));
						}
						sinceLastReport = 0;
						sw.Restart();
					}
					else
					{
						sinceLastReport += bytesRead;
					}

					// See if we need to stop the transfer to limit the size
					if (!ReferenceEquals(limitToBytes, null) && bytesWritten == limitToBytes)
					{
						break;
					}

					// See if we're done
					if (bytesRead < chunkSize)
					{
						// Check if the stream is done
						if (!fs.CanSeek || !(fs is FileStream) || totalBytesWritten >= fs.Length)
						{
							if ((object)Transcoder == null || Transcoder.State != TranscodeState.Active)
							{
								break;
							}
						}

						// Sleep for a bit to prevent a tight loop
						Thread.Sleep(250);
					}
				}
				catch (IOException e)
				{
					if (e.InnerException.GetType() == typeof(System.Net.Sockets.SocketException))
					{
						SocketException se = (SocketException)e.InnerException;
						if (se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset)
						{
							if (logger.IsInfoEnabled) logger.Info("Connection was forcibly closed by the remote host");
						}
					}

					// Break the loop on error
					break;
				}
			}

			sw.Stop();
		}

		// Read in a full line from an input stream
		private string StreamReadLine(Stream inputStream)
		{
			int next_char = 0;
			int readTries = 0;
			string data = "";

			// Loop until newline
			while (true)
			{
				// Read character
				next_char = inputStream.ReadByte();

				// Check for valid character
				if (next_char == -1)
				{
					if (readTries >= 29)
					{
						throw new IOException("ReadByte timed out", null);
					}
					readTries++;
					Thread.Sleep(1);
					continue;
				}
				else
				{
					readTries = 0;
				}

				// Skip carriage returns
				if (next_char == '\r')
				{
					continue;
				}

				// Stop reading on newline
				if (next_char == '\n')
				{
					break;
				}

				// Parse valid characters
				data += Convert.ToChar(next_char);
			}

			// Return the line
			return data;
		}

		// Parse tokens from a HTTP request
		private void ParseRequest()
		{
			try
			{
				string[] tokens = this.StreamReadLine(InputStream).Split(' ');

				// Expects: GET /url HTTP/1.1 or similar
				if (tokens.Length != 3)
				{
					logger.Error("Failed reading HTTP request");
					throw new Exception("Failed reading HTTP request");
				}

				// Store necessary information about this request
				HttpMethod = tokens[0].ToUpper();
				HttpUrl = tokens[1];
				HttpProtocolVersionString = tokens[2];
			}
			// If client disconnects, ignore and continue
			catch (IOException)
			{
			}
			catch (NullReferenceException)
			{
			}
		}

		// Read in all HTTP headers into hashtable
		private void ReadHeaders()
		{
			try
			{
				string line = null;
				while ((line = this.StreamReadLine(InputStream)) != null)
				{
					// Done reading, empty content
					if (line == "" || line.Length = 0)
					{
						return;
					}

					// Check for valid HTTP header format
					int separator = line.IndexOf(':');
					if (separator == -1)
					{
						logger.Error("Failed reading HTTP headers");
						throw new Exception("Failed reading HTTP headers: " + line);
					}

					// Get header name
					string name = line.Substring(0, separator);

					// Find header position
					int pos = separator + 1;

					// Strip any spaces
					while ((pos < line.Length) && (line[pos] == ' '))
					{
						pos++;
					}

					// Store header's value in name key
					this.HttpHeaders[name] = line.Substring(pos, line.Length - pos);
				}
			}
			// If client disconnects, ignore and continue
			catch (IOException)
			{
			}
		}

		// GET requests are ready to go for processing
		private void HandleGETRequest()
		{
			this.ApiProcess();
		}

		// POST requests must have their parameters parsed in order to do API processing
		private const int BUF_SIZE = 4096;
		private void HandlePOSTRequest()
		{
			// this post data processing just reads everything into a memory stream.
			// this is fine for smallish things, but for large stuff we should really
			// hand an input stream to the request processor. However, the input stream
			// we hand him needs to let him see the "end of the stream" at this content
			// length, because otherwise he won't know when he's seen it all!

			int content_len = 0;
			MemoryStream ms = new MemoryStream();
			if (HttpHeaders.ContainsKey("Content-Length"))
			{
				content_len = Convert.ToInt32(HttpHeaders["Content-Length"]);
				if (content_len > MAX_POST_SIZE)
				{
					throw new Exception(String.Format("POST Content-Length({0}) too big for this simple server", content_len));
				}

				byte[] buf = new byte[BUF_SIZE];
				int to_read = content_len;

				while (to_read > 0)
				{
					int numread = InputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
					if (numread == 0)
					{
						if (to_read == 0)
						{
							break;
						}
						else
						{
							throw new Exception("Client disconnected during HTTP POST");
						}
					}

					to_read -= numread;
					ms.Write(buf, 0, numread);
				}

				ms.Seek(0, SeekOrigin.Begin);
			}

			// Add all POST parameters to URL to allow for easy API processing
			this.HttpUrl = this.HttpUrl + "?" + new StreamReader(ms).ReadToEnd();

			// Process the API request
			this.ApiProcess();
		}

		// Process API calls based on the class's HTTP URL
		private void ApiProcess()
		{
			// The API request wrapper
			UriWrapper uri = new UriWrapper(this.HttpUrl);

			// The user who is accessing the API
			User apiUser = null;

			// The handler being accessed
			IApiHandler api = null;

			// No API request found?  Serve web UI
			if (!uri.IsApiCall)
			{
				api = ApiHandlerFactory.CreateApiHandler("web");
				api.Process(uri, apiUser);
				return;
			}

			// Check for valid API action
			if (uri.Action == null)
			{
				api = (ErrorApiHandler)ApiHandlerFactory.CreateApiHandler("error");
				api.Process(uri, apiUser, "Invalid API call");
				return;
			}

			// Log API call
			logger.Info("API: " + this.HttpUrl);

			// Check for session cookie authentication
			string sessionId = this.GetSessionCookie();
			apiUser = ApiAuthenticate.AuthenticateSession(sessionId);

			// If no cookie, try parameter authentication
			if (apiUser == null)
			{
				apiUser = ApiAuthenticate.AuthenticateUri(uri);

				// If user still null, failed authentication, so serve error
				if (apiUser == null)
				{
					api = (ErrorApiHandler)ApiHandlerFactory.CreateApiHandler("error");
					api.Process(uri, apiUser, "Failed to authenticate");
					return;
				}
			}

			// apiUser.SessionId will be generated on new login, so that takes precedence for new session cookie
			sessionId = apiUser.SessionId ?? sessionId;
			this.SetSessionCookie(sessionId);

			// Retrieve the requested API handler by its action
			IApiHandler apiHandler = ApiHandlerFactory.CreateApiHandler(uri.Action);
			apiHandler.Process(uri, apiUser);
		}

		// If a cookie is found, grab it and use it for authentication
		private string GetSessionCookie()
		{
			if (this.HttpHeaders.ContainsKey("Cookie"))
			{
				// Split each cookie into pairs
				string[] cookies = this.HttpHeaders["Cookie"].ToString().Split(new [] {';', ',', '='}, StringSplitOptions.RemoveEmptyEntries);

				// Iterate all cookies
				for (int i = 0; i < cookies.Length; i += 2)
				{
					// Look for wavebox_session cookie
					if (cookies[i] == "wavebox_session")
					{
						return cookies[i + 1];
					}
				}
			}

			return null;
		}

		// Set a new session cookie to be set when the HTTP response is sent
		private void SetSessionCookie(string sessionId)
		{
			if (sessionId != null)
			{
				// Calculate session timeout time (DateTime.Now UTC + SessionTimeout minutes)
				DateTime expire = DateTime.Now.ToUniversalTime().AddMinutes(Injection.Kernel.Get<IServerSettings>().SessionTimeout);

				// Add a delayed header so cookie will be reset on each API call (to prevent timeout)
				this.DelayedHeaders["Set-Cookie"] = String.Format("wavebox_session={0}; Expires={1};", sessionId, expire.ToRFC1123());
			}
		}

		private DateTime CleanLastModified(DateTime? lastModified)
		{
			// If null, use current time
			if (ReferenceEquals(lastModified, null))
			{
				return DateTime.UtcNow;
			}

			// Make sure we're using UTC
			DateTime lastMod = ((DateTime)lastModified).ToUniversalTime();

			// If the time is later than now, use now
			if (DateTime.Compare(DateTime.UtcNow, lastMod) < 0)
			{
				lastMod = DateTime.UtcNow;
			}

			return lastMod;
		}
	}
}
