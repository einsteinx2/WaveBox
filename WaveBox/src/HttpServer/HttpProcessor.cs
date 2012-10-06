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

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/

namespace WaveBox.Http
{
	public class HttpProcessor : IHttpProcessor
	{
		public TcpClient Socket { get; set; }
		public HttpServer Srv { get; set; }

		private Stream InputStream { get; set; }
		public StreamWriter OutputStream { get; set; }

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
			//Console.WriteLine("Send timeout: " + s.SendTimeout);
			//s.SendTimeout = -1;
			//Console.WriteLine("Send timeout after: " + s.SendTimeout);
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
			OutputStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
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
				Console.WriteLine("[HTTPSERVER(1)] " + e);
				WriteErrorHeader();
			}

			try
			{
				OutputStream.Flush();
			}
			catch (IOException e)
			{
				if (e.InnerException.GetType() == typeof(SocketException))
				{
					SocketException se = (SocketException)e.InnerException;
					if (se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset)
					{
						Console.WriteLine("[HTTPSERVER(2)] " + "Connection was forcibly closed by the remote host");
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[HTTPSERVER(3)] " + e);
			}

			// bs.Flush(); // flush any remaining output
			InputStream = null; OutputStream = null; // bs = null;
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

			//Console.WriteLine("starting: " + request);
		}

		public void ReadHeaders() 
		{
			//Console.WriteLine("readHeaders()");
			String line;
			while ((line = streamReadLine(InputStream)) != null)
			{
				if (line.Equals("")) 
				{
					//Console.WriteLine("got headers");
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
				//Console.WriteLine("header: {0}:{1}",name,value);
				HttpHeaders[name] = value;
			}
		}

		public void HandleGETRequest() 
		{
			Stopwatch sw = new Stopwatch();
			IApiHandler apiHandler = ApiHandlerFactory.CreateApiHandler(HttpUrl, this);

			sw.Start();
			apiHandler.Process();
			Console.WriteLine(apiHandler.GetType() + ": {0}ms", sw.ElapsedMilliseconds);
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

			//Console.WriteLine("get post data start");
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
					//Console.WriteLine("starting Read, to_read={0}",to_read);

					int numread = InputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
					//Console.WriteLine("read finished, numread={0}", numread);
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
			Console.WriteLine("[HTTPSERVER] POST request: {0}", data);
			
			Stopwatch sw = new Stopwatch();
			IApiHandler apiHandler = ApiHandlerFactory.CreateApiHandler(data, this);

			sw.Start();
			apiHandler.Process();
			Console.WriteLine(apiHandler.GetType() + ": {0}ms", sw.ElapsedMilliseconds);
			sw.Stop();
		}

		public void WriteErrorHeader() 
		{
			OutputStream.WriteLine("HTTP/1.0 404 File not found");
			OutputStream.WriteLine("Connection: close");
			OutputStream.WriteLine("");
		}

		public void WriteSuccessHeader(long contentLength, string mimeType, IDictionary<string, string> customHeaders)
		{
			OutputStream.WriteLine("HTTP/1.0 200 OK");            
			OutputStream.WriteLine("Content-Type: " + mimeType);
			OutputStream.WriteLine("Content-Length: " + contentLength);
			OutputStream.WriteLine("Access-Control-Allow-Origin: *");
			OutputStream.WriteLine("Connection: close");
			if ((object)customHeaders != null)
			{
				foreach (string key in customHeaders.Keys)
				{
					OutputStream.WriteLine(key + ": " + customHeaders[key]);
				}
			}
			OutputStream.WriteLine("");

			Console.WriteLine("[HTTPSERVER] File header, contentLength: " + contentLength);
		}

		public void WriteText(string text, string mimeType)
		{
			WriteSuccessHeader(UTF8Encoding.Unicode.GetByteCount(text), mimeType + ";charset=utf-8", null);
			OutputStream.Write(text);
		}

		public void WriteJson(string json)
		{
			WriteText(json, "application/json");
		}

		public void WriteFile(Stream fs, int startOffset, long length, string mimeType, IDictionary<string, string> customHeaders)
		{
			if ((object)fs == null || !fs.CanRead || length == 0 || startOffset >= length)
			{ 
				return;
			}

			long contentLength = length - startOffset;
			/*HttpHeader header = null;
			if (fs is FileStream)
			{
				FileInfo fsinfo = new FileInfo(((FileStream)fs).Name);
				header = new HttpHeader(HttpHeader.HttpStatusCode.OK, HttpHeader.ContentTypeForExtension(fsinfo.Extension), contentLength, mimeType);
			}
			else
			{
				header = new HttpHeader(HttpHeader.HttpStatusCode.OK, HttpHeader.HttpContentType.UNKNOWN, length, mimeType);
			}
			header.WriteHeader(OutputStream);*/

			// Write the headers to output stream
			WriteSuccessHeader(contentLength, mimeType, customHeaders);
			OutputStream.Flush();
			Console.WriteLine("[HTTPSERVER] File header, contentLength: {0}, contentType: {1}", contentLength, mimeType);
			//Console.WriteLine("[HTTPSERVER] File header, contentLength: {0}, contentType: {1}, status: {2}", contentLength, header.ContentType, header.StatusCode);

			// Read/Write in 8 KB chunks
			const int chunkSize = 8192;

			// Initialize everything
			byte[] buf = new byte[chunkSize];
			int bytesRead;
			long bytesWritten = 0;
			Stream stream = OutputStream.BaseStream;
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
			while(true)
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
						Console.WriteLine("[SENDFILE]: [ {0} / {1} | {2:F1}% | {3:F1} Mbps ]", bytesWritten, contentLength+startOffset, (Convert.ToDouble(bytesWritten) / Convert.ToDouble(contentLength+startOffset)) * 100, (((double)(sinceLastReport * 8) / 1024) / 1024) / (double)(sw.ElapsedMilliseconds / 1000));
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
				catch (IOException e)
				{
					if (e.InnerException.GetType() == typeof(System.Net.Sockets.SocketException))
					{
						SocketException se = (SocketException)e.InnerException;
						if (se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset)
						{
							Console.WriteLine("[SENDFILE(2)] " + "Connection was forcibly closed by the remote host");
						}
					}

					// Break the loop on error
					break;
				}
			}
			sw.Stop();
			//_sh.writeFailure
		}
	}
}



