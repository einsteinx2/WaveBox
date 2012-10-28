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
using WaveBox.ApiHandler;
using WaveBox.Transcoding;
using WaveBox.Http;
using NLog;

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/

namespace WaveBox.Http
{
	public class HttpProcessor : IHttpProcessor
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public TcpClient Socket { get; set; }
		public HttpServer Srv { get; set; }

		private Stream InputStream { get; set; }
		//public StreamWriter OutputStream { get; set; }
		//public StreamWriter HeaderOutputStream { get; set; }

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
			//logger.Info("Send timeout: " + s.SendTimeout);
			//s.SendTimeout = -1;
			//logger.Info("Send timeout after: " + s.SendTimeout);
			Srv = srv;
		}

		private string streamReadLine(Stream inputStream) 
		{
			int next_char;
			string data = "";
			while (true)
			{
				next_char = inputStream.ReadByte();
				if (next_char == '\n') { break; }
				if (next_char == '\r') { continue; }
				if (next_char == -1) { Thread.Sleep(1); continue; };
				data += Convert.ToChar(next_char);
			}
			return data;
		}

		public void process() 
		{
			// we can't use a StreamReader for input, because it buffers up extra data on us inside it's
			// "processed" view of the world, and we want the data raw after the headers
			InputStream = new BufferedStream(Socket.GetStream());

			// we probably shouldn't be using a streamwriter for all output from handlers either
			try 
			{
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
			catch (Exception e) 
			{
				logger.Info("[HTTPSERVER(1)] " + e);
				WriteErrorHeader();
			}

			InputStream = null;
			Socket.Close();             
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

			//logger.Info("starting: " + request);
		}

		public void ReadHeaders() 
		{
			//logger.Info("readHeaders()");
			String line;
			while ((line = streamReadLine(InputStream)) != null)
			{
				if (line.Equals("")) 
				{
					//logger.Info("got headers");
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
				//logger.Info("header: {0}:{1}",name,value);
				HttpHeaders[name] = value;
			}
		}

		public void HandleGETRequest() 
		{
			Stopwatch sw = new Stopwatch();
			IApiHandler apiHandler = ApiHandlerFactory.CreateApiHandler(HttpUrl, this);

			sw.Start();
			apiHandler.Process();
			//logger.Info(apiHandler.GetType() + ": {0}ms", sw.ElapsedMilliseconds);
			sw.Stop();
		}

		private const int BUF_SIZE = 4096;
		public void HandlePOSTRequest()
		{
			// this post data processing just reads everything into a memory stream.
			// this is fine for smallish things, but for large stuff we should really
			// hand an input stream to the request processor. However, the input stream 
			// we hand him needs to let him see the "end of the stream" at this content 
			// length, because otherwise he won't know when he's seen it all! 

			//logger.Info("get post data start");
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
					//logger.Info("starting Read, to_read={0}",to_read);

					int numread = InputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
					//logger.Info("read finished, numread={0}", numread);
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
			//logger.Info("[HTTPSERVER] POST request: {0}", data);
			
			Stopwatch sw = new Stopwatch();
			IApiHandler apiHandler = ApiHandlerFactory.CreateApiHandler(data, this);

			sw.Start();
			apiHandler.Process();
			//logger.Info(apiHandler.GetType() + ": {0}ms", sw.ElapsedMilliseconds);
			sw.Stop();
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
			outStream.WriteLine("Connection: close");
			outStream.WriteLine("");
			outStream.Flush();
			
			logger.Info("[HTTPSERVER] Success header, contentLength: " + contentLength);
		}

		public void WriteText(string text, string mimeType)
		{
			// Makes no sense at all, but for whatever reason, all ajax calls fail with a cross site 
			// scripting error if Content-Type is set, but the player needs it for files for seeking,
			// so pass -1 for no Content-Length header for all text requests
			//
			//WriteSuccessHeader(UTF8Encoding.Unicode.GetByteCount(text), mimeType + ";charset=utf-8", null);
			WriteSuccessHeader(-1, mimeType + ";charset=utf-8", null);

			StreamWriter outStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
			outStream.Write(text);
			outStream.Flush();
		}

		public void WriteJson(string json)
		{
			WriteText(json, "application/json");
		}

		public void WriteFile(Stream fs, int startOffset, long length, string mimeType, IDictionary<string, string> customHeaders, bool isSendContentLength)
		{
			if ((object)fs == null || !fs.CanRead || length == 0 || startOffset >= length)
			{ 
				return;
			}

			long contentLength = length - startOffset;

			WriteSuccessHeader(isSendContentLength ? contentLength : -1, mimeType, customHeaders);
			//OutputStream.Flush();
			logger.Info("[HTTPSERVER] File header, contentLength: {0}, contentType: {1}, lastMod: {2}", contentLength, mimeType, customHeaders != null && customHeaders.ContainsKey("Last-Modified") ? customHeaders["Last-Modified"] : String.Empty);
			//logger.Info("[HTTPSERVER] File header, contentLength: {0}, contentType: {1}, status: {2}", contentLength, header.ContentType, header.StatusCode);

			// Read/Write in 8 KB chunks
			const int chunkSize = 8192;

			// Initialize everything
			byte[] buf = new byte[chunkSize];
			int bytesRead;
			long bytesWritten = 0;
			Stream stream = new BufferedStream(Socket.GetStream());//OutputStream.BaseStream;
			int sinceLastReport = 0;
			Stopwatch sw = new Stopwatch();

			if (fs.CanSeek)// && startOffset < fs.Length)
			{
				// Seek to the start offset
				fs.Seek(startOffset, SeekOrigin.Begin);
				bytesWritten = fs.Position;
			}
			/*else
			{
				// This part doesn't exist, so bail
				return;
			}*/

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
						logger.Info("[SENDFILE]: [ {0} / {1} | {2:F1}% | {3:F1} Mbps ]", bytesWritten, contentLength + startOffset, (Convert.ToDouble(bytesWritten) / Convert.ToDouble(contentLength + startOffset)) * 100, (((double)(sinceLastReport * 8) / 1024) / 1024) / (double)(sw.ElapsedMilliseconds / 1000));
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
							logger.Info("[SENDFILE(2)] " + "Connection was forcibly closed by the remote host");
						}
					}

					// Break the loop on error
					break;
				}
			}
			sw.Stop();
			//_sh.writeFailure
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



