using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WaveBox.ApiHandler;
using System.Threading;
using System.Diagnostics;

namespace WaveBox.HttpServer
{
	public class WaveBoxHttpServer : HttpServer
	{
		public WaveBoxHttpServer(int port) : base(port)
		{
		}

		public override void HandleGETRequest(HttpProcessor processor)
		{
			var sw = new Stopwatch();
			var apiHandler = ApiHandlerFactory.CreateApiHandler(processor.HttpUrl, processor);

			sw.Start();
			apiHandler.Process();
			Console.WriteLine(apiHandler.GetType() + ": {0}ms", sw.ElapsedMilliseconds);
			sw.Stop();
		}

		public override void HandlePOSTRequest(HttpProcessor processor, StreamReader inputData)
		{
			string data = processor.HttpUrl + "?" + inputData.ReadToEnd();
			Console.WriteLine("[HTTPSERVER] POST request: {0}", data);
			
			var sw = new Stopwatch();
			var apiHandler = ApiHandlerFactory.CreateApiHandler(data, processor);

			sw.Start();
			apiHandler.Process();
			Console.WriteLine(apiHandler.GetType() + ": {0}ms", sw.ElapsedMilliseconds);
			sw.Stop();
		}

		public static void sendJson(HttpProcessor processor, string json)
		{
			processor.WriteSuccess();
			processor.OutputStream.Write(json);
		}

		public static void sendFile (HttpProcessor processor, FileStream fs, int startOffset, long length)
		{
			if ((object)fs == null || length == 0 || startOffset >= length) 
				return;

			FileInfo fsinfo = new FileInfo(fs.Name);

			// Write the headers to output stream
			long contentLength = length - startOffset;
			WaveBoxHttpHeader h = new WaveBoxHttpHeader(WaveBoxHttpHeader.HttpStatusCode.OK, "", contentLength);
			h.WriteHeader(processor);

			// Read/Write in 8 KB chunks
			const int chunkSize = 8192;

			// Initialize everything
			byte[] buf = new byte[chunkSize];
			int bytesRead;
			long bytesWritten = 0;
			var stream = processor.OutputStream.BaseStream;
			int sinceLastReport = 0;
			var sw = new Stopwatch();

			// Seek to the start offset
			fs.Seek(startOffset, SeekOrigin.Begin);
			bytesWritten = fs.Position;

			sw.Start();
			while(true)
			{
				try
				{
					// Attempt to read a chunk
					bytesRead = fs.Read(buf, 0, chunkSize);

					// Send the bytes out to the client
					if (bytesRead > 0)
					{
						stream.Write(buf, 0, bytesRead);
						bytesWritten += bytesRead;
					}

					// Log the progress (only for testing)
					if (sw.ElapsedMilliseconds > 1000)
					{
						Console.WriteLine("[SENDFILE] " + fsinfo.Name + ": [ {0} / {1} | {2:F1}% | {3:F1} Mbps ]", bytesWritten, contentLength+startOffset, (Convert.ToDouble(bytesWritten) / Convert.ToDouble(contentLength+startOffset)) * 100, (((double)(sinceLastReport * 8) / 1024) / 1024) / (double)(sw.ElapsedMilliseconds / 1000));
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
						Thread.Sleep(2000);

						// Check if the file is done
						if (bytesWritten >= fs.Length)
						{
							// We've written the whole file, so break
							Console.WriteLine("[SENDFILE] " + fsinfo.Name + ": Done.");
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

		public void sendBytes(HttpProcessor processor, byte[] bytes, int startOffset, long length)
		{
			if ((object)bytes == null || length == 0 || startOffset >= length)
				return;

			// Write the headers to output stream
			long contentLength = length - startOffset;
			WaveBoxHttpHeader h = new WaveBoxHttpHeader(WaveBoxHttpHeader.HttpStatusCode.OK, "", contentLength);
			h.WriteHeader(processor);

			// Read/Write in 8 KB chunks
			const int chunkSize = 8192;

			// Initialize everything
			int bytesRead = 0;
			long bytesWritten = 0;
			Stream stream = processor.OutputStream.BaseStream;
			int sinceLastReport = 0;
			Stopwatch sw = new Stopwatch();

			// Seek to the start offset
			bytesWritten = startOffset;

			sw.Start();
			while(true)
			{
				try
				{
					// Either read the chunk size, or whatever's left
					bytesRead = (int)(bytesWritten <= contentLength - chunkSize ? chunkSize : contentLength - bytesWritten);

					// Send the bytes out to the client
					// TODO: need to handle files larger than max int
					stream.Write(bytes, (int)bytesWritten, bytesRead);
					bytesWritten += bytesRead;

					// Log the progress (only for testing)
					if (sw.ElapsedMilliseconds > 1000)
					{
						Console.WriteLine("[SENDBYTES]: [ {0} / {1} | {2:F1}% | {3:F1} Mbps ]", bytesWritten, contentLength+startOffset, (Convert.ToDouble(bytesWritten) / Convert.ToDouble(contentLength+startOffset)) * 100, (((double)(sinceLastReport * 8) / 1024) / 1024) / (double)(sw.ElapsedMilliseconds / 1000));
						sinceLastReport = 0;
						sw.Restart();
					}
					else
					{
						sinceLastReport += bytesRead;
					}

					// See if we're done
					if (bytesWritten >= length)
					{
						// We've written all the bytes, so break
						Console.WriteLine("[SENDFILE]: Done.");
						break;
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
