using System;
using System.IO;
using System.Collections.Generic;
using Ninject;
using WaveBox.Core.Injection;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	public class WebInterfaceHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		// Define root project directory containing users's web interface, or "theme"
		private static string webRoot = ServerUtility.RootPath() + "themes" + Path.DirectorySeparatorChar;

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
			string path = webRoot;

			// Check for and apply theme path
			if (Injection.Kernel.Get<IServerSettings>().Theme == null)
			{
				logger.Error("No theme set in WaveBox configuration, cannot serve Web UI");

				// File not found
				Processor.WriteErrorHeader();
				return;
			}

			// Append theme path to web root
			path += Injection.Kernel.Get<IServerSettings>().Theme;

			// Validate theme path
			if (!Directory.Exists(path))
			{
				logger.Error("Invalid theme '" + Injection.Kernel.Get<IServerSettings>().Theme + "' set in WaveBox configuration, cannot serve Web UI");

				// File not found
				Processor.WriteErrorHeader();
				return;
			}

			if (Uri.UriParts.Count == 0)
			{
				// No path, so return the home page
				path += Path.DirectorySeparatorChar + "index.html";

				// Ensure theme contains an index
				if (!File.Exists(path))
				{
					logger.Error("Theme '" + Injection.Kernel.Get<IServerSettings>().Theme + "' missing required file index.html");

					// File not found
					Processor.WriteErrorHeader();
					return;
				}
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

			// Make sure the file exists
			if (!File.Exists(path))
			{
				if (logger.IsInfoEnabled) logger.Info("File does not exist: " + path);

				// File not found
				Processor.WriteErrorHeader();
				return;
			}

			if (logger.IsInfoEnabled) logger.Info("Serving file: " + path);

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

			Processor.WriteFile(file, startOffset, length, HttpHeader.MimeTypeForExtension(Path.GetExtension(path)), null, true, new FileInfo(path).LastWriteTimeUtc);
			file.Close();
		}
	}
}

