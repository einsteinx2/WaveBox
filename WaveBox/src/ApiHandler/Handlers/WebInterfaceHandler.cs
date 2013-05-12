using System;
using System.IO;
using System.Collections.Generic;
using WaveBox.TcpServer.Http;

namespace WaveBox.ApiHandler.Handlers
{
	public class WebInterfaceHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		// Define root project directory containing web interface
		private const string rootPath = "html";

		/// <summary>
		/// Constructor for WebInterfaceHandler
		/// </summary>
		public WebInterfaceHandler(UriWrapper uri, IHttpProcessor processor)
		{
			Uri = uri;
			Processor = processor;
		}

		/// <summary>
		/// Process returns a page from the WaveBox web interface
		/// </summary>
		public void Process()
		{
			// Store root path, return index by default
			string path = rootPath;
			if (Uri.UriParts.Count == 0)
			{
				// No path, so return the home page
				path += Path.DirectorySeparatorChar + "index.html";
			}
			else
			{
				// Iterate UriParts to send web pages
				for (int i = 0; i < Uri.UriParts.Count; i++)
				{
					string pathPart = Uri.UriParts[i];
					if (pathPart.Length > 0 && pathPart[0] == '.')
					{
						// Do not return hidden files/folders
						Processor.WriteErrorHeader();
						return;
					}
					else
					{
						path += Path.DirectorySeparatorChar + Uri.UriParts[i];
					}
				}
			}

			if (logger.IsInfoEnabled) logger.Info("path: " + path);

			// Make sure the file exists
			if (File.Exists(path))
			{
				// If it exists, check to see if the headers contains an If-Modified-Since entry
				if (Processor.HttpHeaders.ContainsKey("If-Modified-Since"))
				{
					if (logger.IsInfoEnabled) logger.Info(Processor.HttpHeaders["If-Modified-Since"]);

					// Took me a while to figure this out, but even if the time zone in the request is GMT, DateTime.Parse converts it to local time.
					var ims = DateTime.Parse(Processor.HttpHeaders["If-Modified-Since"].ToString());
					var lastMod = File.GetLastWriteTime(path);

					if (ims >= lastMod)
					{
						if (logger.IsInfoEnabled) logger.Info("File not modified: " + path);
						Processor.WriteNotModifiedHeader();
						return;
					}
				}

				if (logger.IsInfoEnabled) logger.Info("serving file at path: " + path);

				// Serve up files inside html directory
				FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
				int startOffset = 0;

				// Handle the Range header to start from later in the file
				if (Processor.HttpHeaders.ContainsKey("Range"))
				{
					string range = (string)Processor.HttpHeaders["Range"];
					string start = range.Split(new char[]{'-', '='})[1];
					if (logger.IsInfoEnabled) logger.Info("Connection retried.  Resuming from " + start);
					startOffset = Convert.ToInt32(start);
				}

				long length = file.Length - startOffset;

				var dict = new Dictionary<string, string>();
				var lwt = HttpProcessor.DateTimeToLastMod(new FileInfo(path).LastWriteTimeUtc);
				dict.Add("Last-Modified", lwt);

				Processor.WriteFile(file, startOffset, length, HttpHeader.MimeTypeForExtension(Path.GetExtension(path)), dict, true);
				file.Close();
			}
			else
			{
				if (logger.IsInfoEnabled) logger.Info("file at path does not exist: " + path);

				// File not found
				Processor.WriteErrorHeader();
			}
		}
	}
}

