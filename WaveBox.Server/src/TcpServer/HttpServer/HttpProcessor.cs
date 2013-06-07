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

		// Delayed headers, mostly used for updating sessions if needed
		public Dictionary<string, string> DelayedHeaders = new Dictionary<string, string>();

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
				logger.Error("Failed reading HTTP request");
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
					logger.Error("Failed reading HTTP headers");
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
			outStream.WriteLine("HTTP/1.0 404 File not found");
			outStream.WriteLine("Connection: close");
			outStream.WriteLine("");
			outStream.Flush();
		}

		public void WriteSuccessHeader(long contentLength, string mimeType, IDictionary<string, string> customHeaders)
		{
			StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
			outStream.WriteLine("HTTP/1.0 200 OK");
			outStream.WriteLine("Content-Type: " + mimeType);
			if (contentLength >= 0)
			{
				outStream.WriteLine("Content-Length: " + contentLength);
			}
			outStream.WriteLine("Access-Control-Allow-Origin: *");
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
			
			if (logger.IsInfoEnabled) logger.Info("Success header, contentLength: " + contentLength);
		}

		public void WriteText(string text, string mimeType)
		{
			WriteText(text, mimeType, null);
		}

		public void WriteText(string text, string mimeType, IDictionary<string, string> customHeaders)
		{
			// Makes no sense at all, but for whatever reason, all ajax calls fail with a cross site 
			// scripting error if Content-Type is set, but the player needs it for files for seeking,
			// so pass -1 for no Content-Length header for all text requests
			WriteSuccessHeader(Encoding.UTF8.GetByteCount(text) + 3, mimeType + ";charset=utf-8", customHeaders);
			StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()), Encoding.UTF8);
			outStream.Write(text);
			outStream.Flush();
		}

		public void WriteJson(string json)
		{
			WriteText(json, "application/json");
		}

		public void WriteJson(string json, IDictionary<string, string> customHeaders)
		{
			WriteText(json, "application/json", customHeaders);
		}

		public void WriteFile(Stream fs, int startOffset, long length, string mimeType, IDictionary<string, string> customHeaders, bool isSendContentLength)
		{
			if ((object)fs == null || !fs.CanRead || length == 0 || startOffset >= length)
			{ 
				return;
			}

			long contentLength = length - startOffset;

			WriteSuccessHeader(isSendContentLength ? contentLength : -1, mimeType, customHeaders);
			if (logger.IsInfoEnabled) logger.Info("File header, contentLength: " + contentLength + ", contentType: " + mimeType + ", lastMod: " + (customHeaders != null && customHeaders.ContainsKey("Last-Modified") ? customHeaders["Last-Modified"] : String.Empty));

			// Read/Write in 8 KB chunks
			const int chunkSize = 8192;

			// Initialize everything
			byte[] buf = new byte[chunkSize];
			int bytesRead;
			long bytesWritten = 0;
			Socket.SendTimeout = 30000;
			Stream stream = new BufferedStream(Socket.GetStream());
			int sinceLastReport = 0;
			Stopwatch sw = new Stopwatch();

			if (fs.CanSeek)
			{
				// Seek to the start offset
				fs.Seek(startOffset, SeekOrigin.Begin);
				bytesWritten = fs.Position;
			}

			sw.Start();
			while (true)
			{
				try
				{
					// Attempt to read a chunk
					bytesRead = fs.Read(buf, 0, chunkSize);

					// Send the bytes out to the client
					stream.Write(buf, 0, bytesRead);
					stream.Flush();
					bytesWritten += bytesRead;

					// Log the progress (only for testing)
					if (sw.ElapsedMilliseconds > 1000)
					{
						if (logger.IsInfoEnabled)
						{
							logger.Info(String.Format("[ {0,10} / {1,10} | {2:000}% | {3:00.00000} Mbps ]",
								bytesWritten,
								(contentLength + startOffset),
								((Convert.ToDouble(bytesWritten) / Convert.ToDouble(contentLength + startOffset)) * 100),
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

		public static string DateTimeToLastMod(DateTime theDate)
		{
			string dayOfWeek;

			//var offset = TimeZone.CurrentTimeZone.GetUtcOffset(theDate).Ticks;
			//var theDateUtc = theDate.AddTicks(-offset);
			var d = theDate.DayOfWeek;
			if(d == DayOfWeek.Sunday)
				dayOfWeek = "Sun";
			else if(d == DayOfWeek.Monday)
				dayOfWeek = "Mon";
			else if(d == DayOfWeek.Tuesday)
				dayOfWeek = "Tue";
			else if(d == DayOfWeek.Wednesday)
				dayOfWeek = "Wed";
			else if(d == DayOfWeek.Thursday)
				dayOfWeek = "Thu";
			else if(d == DayOfWeek.Friday)
				dayOfWeek = "Fri";
			else dayOfWeek = "Sat";

			string month;
			var m = theDate.Month;
			if(m == 1)
				month = "Jan";
			else if(m == 2)
				month = "Feb";
			else if(m == 3)
				month = "Mar";
			else if(m == 4)
				month = "Apr";
			else if(m == 5)
				month = "May";
			else if(m == 6)
				month = "Jun";
			else if(m == 7)
				month = "Jul";
			else if(m == 8)
				month = "Aug";
			else if(m == 9)
				month = "Sep";
			else if(m == 10)
				month = "Oct";
			else if(m == 11)
				month = "Nov";
			else month = "Dec";

			return dayOfWeek + ", " + theDate.Day + " " + month + " " + theDate.Year + " " + string.Format("{0:HH}:{0:mm}:{0:ss}", theDate) + " GMT";
		}
	}
}



