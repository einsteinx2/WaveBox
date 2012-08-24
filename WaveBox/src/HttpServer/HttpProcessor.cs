using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WaveBox;
using System.Diagnostics;
using WaveBox.ApiHandler;

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
				Console.WriteLine("[HTTPSERVER(1)] " + e.ToString());
				WriteErrorHeader();
			}

			try
			{
				OutputStream.Flush();
			}
			catch (IOException e)
			{
				if (e.InnerException.GetType() == typeof(System.Net.Sockets.SocketException))
				{
					var se = (System.Net.Sockets.SocketException)e.InnerException;
					if (se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset)
					{
						Console.WriteLine("[HTTPSERVER(2)] " + "Connection was forcibly closed by the remote host");
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[HTTPSERVER(3)] " + e.ToString());
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
			var sw = new Stopwatch();
			var apiHandler = ApiHandlerFactory.CreateApiHandler(HttpUrl, this);

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
			
			var sw = new Stopwatch();
			var apiHandler = ApiHandlerFactory.CreateApiHandler(data, this);

			sw.Start();
			apiHandler.Process();
			Console.WriteLine(apiHandler.GetType() + ": {0}ms", sw.ElapsedMilliseconds);
			sw.Stop();
		}

		public void WriteJsonHeader()
		{
			OutputStream.WriteLine("HTTP/1.0 200 OK");            
			OutputStream.WriteLine("Content-Type: application/json;charset=utf-8");
			OutputStream.WriteLine("Access-Control-Allow-Origin: *");
			OutputStream.WriteLine("Connection: close");
			OutputStream.WriteLine("");
		}

		public void WriteErrorHeader() 
		{
			OutputStream.WriteLine("HTTP/1.0 404 File not found");
			OutputStream.WriteLine("Connection: close");
			OutputStream.WriteLine("");
		}

		public void WriteFileHeader(long contentLength)
		{
			OutputStream.WriteLine("HTTP/1.0 200 OK");            
			//OutputStream.WriteLine("Content-Type: application/json;charset=utf-8");
			OutputStream.WriteLine("Content-Length: " + contentLength);
			OutputStream.WriteLine("Access-Control-Allow-Origin: *");
			OutputStream.WriteLine("Connection: close");
			OutputStream.WriteLine("");

			Console.WriteLine("[HTTPSERVER] File header, contentLength: " + contentLength);
		}

		public void WriteJson(string json)
		{
			WriteJsonHeader();
			OutputStream.Write(json);
		}

		public void WriteFile (Stream fs, int startOffset, long length)
		{
			if ((object)fs == null || !fs.CanRead || length == 0 || startOffset >= length) 
				return;

			HttpHeader header = null;
			long contentLength = length - startOffset;
			if (fs is FileStream) 
			{
				FileInfo fsinfo = new FileInfo (((FileStream)fs).Name);

				// Write the headers to output stream

				header = new HttpHeader (HttpHeader.HttpStatusCode.OK, HttpHeader.ContentTypeForExtension(fsinfo.Extension), contentLength);
			} 
			else
			{
				header = new HttpHeader (HttpHeader.HttpStatusCode.OK, HttpHeader.HttpContentType.UNKNOWN, length);
			}

			header.WriteHeader (OutputStream);
            OutputStream.Flush();
			Console.WriteLine ("[HTTPSERVER] File header, contentLength: {0}, contentType: {1}, status: {2}", contentLength, header.ContentType, header.StatusCode);

			// Read/Write in 8 KB chunks
			const int chunkSize = 8192;

			// Initialize everything
			byte[] buf = new byte[chunkSize];
			int bytesRead;
			long bytesWritten = 0;
			var stream = OutputStream.BaseStream;
			int sinceLastReport = 0;
			var sw = new Stopwatch ();

			if (fs.CanSeek) 
			{
				// Seek to the start offset
				fs.Seek (startOffset, SeekOrigin.Begin);
				bytesWritten = fs.Position;
			}

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
						// We read less than we asked for from the file
						// Sleep 2 seconds and then see if the file grew
						if (fs is FileStream)
                        {
                            Thread.Sleep(2);
                        }

						// Check if the stream is done
						if (bytesWritten >= fs.Length || !(fs is FileStream))
						{
							// We've written the whole file, so flush the buffer and break
							Console.WriteLine("[SENDFILE]: Done.");
							break;
						}
					}
				}
				catch (IOException e)
				{
					if (e.InnerException.GetType() == typeof(System.Net.Sockets.SocketException))
					{
						var se = (System.Net.Sockets.SocketException)e.InnerException;
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



