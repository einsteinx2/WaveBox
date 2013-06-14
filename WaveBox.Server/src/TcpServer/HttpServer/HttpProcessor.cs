using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WaveBox;
using System.Diagnostics;
using WaveBox.Transcoding;
using WaveBox.TcpServer.Http;

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/
using WaveBox.ApiHandler;
using WaveBox.Static;

namespace WaveBox.TcpServer.Http
{
	public class HttpProcessor : IHttpProcessor
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public TcpClient Socket { get; set; }
		public HttpServer Srv { get; set; }

		private Stream InputStream { get; set; }

		public String HttpMethod { get; set; }
		public String HttpUrl { get; set; }
		public String HttpProtocolVersionString { get; set; }
		public Hashtable HttpHeaders { get; set; }

		public ITranscoder Transcoder { get; set; }

		private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

		public HttpProcessor(TcpClient s, HttpServer srv) 
		{
			HttpHeaders = new Hashtable();
			Socket = s;
			Srv = srv;
		}

		private string streamReadLine(Stream inputStream) 
		{
			int next_char, readTries = 0;
			string data = "";
			while (true)
			{
				next_char = inputStream.ReadByte();

				if (next_char == -1) 
				{
					if (readTries >= 29)
					{
						throw new Exception("ReadByte timed out", null);
					}
					readTries++;
					Thread.Sleep(1); 
					continue; 
				}
				else
				{
					readTries = 0;
				}

				if (next_char == '\n') { break; }
				if (next_char == '\r') { continue; }

				data += Convert.ToChar(next_char);
			}
			return data;
		}

		public void process()
		{
			// we can't use a StreamReader for input, because it buffers up extra data on us inside it's
			// "processed" view of the world, and we want the data raw after the headers
			InputStream = Socket.GetStream();

			// we probably shouldn't be using a streamwriter for all output from handlers either
			try
			{
				InputStream.ReadTimeout = 30000;
				ParseRequest();
				ReadHeaders();
				if (HttpMethod.Equals("GET"))
				{
					HandleGETRequest();
				}
				else if (HttpMethod.Equals("POST"))
				{
					HandlePOSTRequest();
				}
			}
			catch
			{
				logger.Error("Received malformed URL from client");
				WriteErrorHeader();
			}
			finally
			{
				InputStream = null;
				Socket.GetStream().Close();
				Socket.Client.Close();
				Socket.Close();
			}
		}

		public void ParseRequest() 
		{
			String request = streamReadLine(InputStream);
			string[] tokens = request.Split(' ');
			if (tokens.Length != 3) 
			{
				throw new Exception("invalid http request line");
			}
			HttpMethod = tokens[0].ToUpper();
			HttpUrl = tokens[1];
			HttpProtocolVersionString = tokens[2];
		}

		public void ReadHeaders() 
		{
			String line;
			while ((line = streamReadLine(InputStream)) != null)
			{
				if (line.Equals("")) 
				{
					return;
				}
				
				int separator = line.IndexOf(':');
				if (separator == -1) 
				{
					throw new Exception("invalid http header line: " + line);
				}
				String name = line.Substring(0, separator);
				int pos = separator + 1;
				while ((pos < line.Length) && (line[pos] == ' ')) 
				{
					pos++; // strip any spaces
				}
					
				string value = line.Substring(pos, line.Length - pos);
				HttpHeaders[name] = value;
			}
		}

		public void HandleGETRequest() 
		{
			IApiHandler apiHandler = ApiHandlerFactory.CreateApiHandler(HttpUrl, this);

			apiHandler.Process();
		}

		private const int BUF_SIZE = 4096;
		public void HandlePOSTRequest()
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
							throw new Exception("client disconnected during post");
						}
					 }
					 to_read -= numread;
					 ms.Write(buf, 0, numread);
				 }
				 ms.Seek(0, SeekOrigin.Begin);
			}

			string data = HttpUrl + "?" + new StreamReader(ms).ReadToEnd();
			
			IApiHandler apiHandler = ApiHandlerFactory.CreateApiHandler(data, this);

			apiHandler.Process();
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
			outStream.WriteLine("HTTP/1.1 404 File not found");
			outStream.WriteLine("Connection: close");
			outStream.WriteLine("");
			outStream.Flush();
		}

		public void WriteSuccessHeader(long contentLength, string mimeType, IDictionary<string, string> customHeaders, DateTime lastModified, bool isPartial = false)
		{
			StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
			string status = isPartial ? "HTTP/1.1 206 Partial Content" : "HTTP/1.1 200 OK";
			outStream.WriteLine(status);
			outStream.WriteLine("Date: " + DateTime.UtcNow.ToRFC1123());
			outStream.WriteLine("Server: WaveBox/" + WaveBoxService.BuildVersion);
			outStream.WriteLine("Last-Modified: " + lastModified.ToRFC1123());
			outStream.WriteLine("ETag: \"" + CreateETagString(lastModified) + "\"");
			outStream.WriteLine("Accept-Ranges: bytes");
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
			//outStream.WriteLine("Connection: close");
			outStream.WriteLine("");
			outStream.Flush();
			
			if (logger.IsInfoEnabled) logger.Info("Success header, status: " + status + " contentLength: " + contentLength + " ETag: " + CreateETagString(lastModified) + " Last-Modified: " + lastModified.ToRFC1123());
		}

		public void WriteText(string text, string mimeType)
		{
			// Makes no sense at all, but for whatever reason, all ajax calls fail with a cross site 
			// scripting error if Content-Type is set, but the player needs it for files for seeking,
			// so pass -1 for no Content-Length header for all text requests
			WriteSuccessHeader(Encoding.UTF8.GetByteCount(text) + 3, mimeType + ";charset=utf-8", null, DateTime.UtcNow);
			StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()), Encoding.UTF8);
			outStream.Write(text);
			outStream.Flush();
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
				if (logger.IsInfoEnabled) logger.Info("If-Modified-Since header: " + HttpHeaders["If-Modified-Since"]);

				if (HttpHeaders["If-Modified-Since"].Equals(lastMod.ToRFC1123()))
				{
					WriteNotModifiedHeader();
					return;
				}
			}
			if (HttpHeaders.ContainsKey("If-None-Match"))
			{
				if (logger.IsInfoEnabled) logger.Info("If-None-Match header: " + HttpHeaders["If-None-Match"]);

				if (HttpHeaders["If-None-Match"].Equals(CreateETagString(lastMod)))
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
				if (logger.IsInfoEnabled) logger.Info("Trying to seek to " + startOffset);

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

				if (logger.IsInfoEnabled) logger.Info("actual start offset " + actualStartOffset);

				totalBytesWritten = fs.Position;
			}

			// TODO: make sure content length is correct when doing range requests on transcoded files
			long contentLength = length - actualStartOffset;
			if (!ReferenceEquals(limitToBytes, null) && contentLength > limitToBytes)
				contentLength = (long)limitToBytes;

			bool isPartial = startOffset != 0 || !ReferenceEquals(limitToBytes, null);
			if (isPartial)
			{
				if (ReferenceEquals(customHeaders, null))
					customHeaders = new Dictionary<string, string>();

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
						if (!fs.CanSeek || !(fs is FileStream) || bytesWritten >= fs.Length)
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
				catch(IOException e)
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

		private DateTime CleanLastModified(DateTime? lastModified)
		{
			// If null, use current time
			if (ReferenceEquals(lastModified, null))
				return DateTime.UtcNow;

			// Make sure we're using UTC
			DateTime lastMod = ((DateTime)lastModified).ToUniversalTime();

			// If the time is later than now, use now
			if (DateTime.Compare(DateTime.UtcNow, lastMod) < 0)
				lastMod = DateTime.UtcNow;

			return lastMod;
		}
		
		private string CreateETagString(DateTime lastModified)
		{
			return lastModified.ToRFC1123().SHA1();
		}
	}
}



