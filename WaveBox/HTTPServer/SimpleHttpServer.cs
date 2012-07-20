using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WaveBox;

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/

namespace Bend.Util 
{
	public class HttpProcessor 
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
			} catch (Exception e) {
				Console.WriteLine("Exception: " + e.ToString());
				WriteFailure();
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
						Console.WriteLine("[HTTPSERVER] " + "Connection was forcibly closed by the remote host");
					}
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
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
			while ((line = streamReadLine(InputStream)) != null) {
				if (line.Equals("")) {
					//Console.WriteLine("got headers");
					return;
				}
				
				int separator = line.IndexOf(':');
				if (separator == -1) {
					throw new Exception("invalid http header line: " + line);
				}
				String name = line.Substring(0, separator);
				int pos = separator + 1;
				while ((pos < line.Length) && (line[pos] == ' ')) {
					pos++; // strip any spaces
				}
					
				string value = line.Substring(pos, line.Length - pos);
				//Console.WriteLine("header: {0}:{1}",name,value);
				HttpHeaders[name] = value;
			}
		}

		public void HandleGETRequest() 
		{
			Srv.HandleGETRequest(this);
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
			//Console.WriteLine("get post data end");
			Srv.HandlePOSTRequest(this, new StreamReader(ms));
		}

		public void WriteSuccess()
		{
			OutputStream.WriteLine("HTTP/1.0 200 OK");            
			OutputStream.WriteLine("Content-Type: application/json;charset=utf-8");
			OutputStream.WriteLine("Access-Control-Allow-Origin: *");
			OutputStream.WriteLine("Connection: close");
			OutputStream.WriteLine("");
		}

		public void WriteFailure() 
		{
			OutputStream.WriteLine("HTTP/1.0 404 File not found");
			OutputStream.WriteLine("Connection: close");
			OutputStream.WriteLine("");
		}
	}

	public abstract class HttpServer 
	{
		protected int Port { get; set; }
		private TcpListener Listener { get; set; }
		private bool IsActive { get; set; }
	   
		public HttpServer(int port) 
		{
			IsActive = true;
			Port = port;
		}

		public void Listen()
		{
			Listener = new TcpListener(IPAddress.Any, Port);
			try
			{
				Listener.Start();
			}
			catch (System.Net.Sockets.SocketException e)
			{
				if (e.SocketErrorCode.ToString() == "AddressAlreadyInUse")
				{
					Console.WriteLine("ERROR: Socket already in use.  Ensure that PMS is not already running.");
				}

				else Console.WriteLine("ERROR: " + e.Message);
				Program.ShutdownCommon();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				Program.ShutdownCommon();
			}
			Console.WriteLine("HTTP server started");
			while (IsActive) 
			{                
				TcpClient s = Listener.AcceptTcpClient();
				//TcpClient d = listener.BeginAcceptTcpClient
				HttpProcessor processor = new HttpProcessor(s, this);
				Thread thread = new Thread(new ThreadStart(processor.process));
				thread.Start();
				Thread.Sleep(1);
			}
		}

		public abstract void HandleGETRequest(HttpProcessor p);
		public abstract void HandlePOSTRequest(HttpProcessor p, StreamReader inputData);
	}
}



