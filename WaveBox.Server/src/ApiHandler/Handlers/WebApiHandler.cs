using System;
using System.IO;
using System.Collections.Generic;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	public class WebApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "web"; } }

		// API handler is read-only, so no permissions checks needed
		public bool CheckPermission(User user, string action)
		{
			return true;
		}

		// Define root project directory containing web interfaces, or "themes"
		private static string webRoot = ServerUtility.ExecutablePath() + "themes" + Path.DirectorySeparatorChar;

		/// <summary>
		/// Process returns a page from the WaveBox web interface
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Store root path, return index by default
			string path = webRoot;

			// Check for and apply theme path
			if (Injection.Kernel.Get<IServerSettings>().Theme == null)
			{
				logger.Error("No theme set in WaveBox configuration, cannot serve Web UI");

				// File not found
				processor.WriteErrorHeader();
				return;
			}

			// Append theme path to web root
			path += Injection.Kernel.Get<IServerSettings>().Theme;

			// Validate theme path
			if (!Directory.Exists(path))
			{
				logger.Error("Invalid theme '" + Injection.Kernel.Get<IServerSettings>().Theme + "' set in WaveBox configuration, cannot serve Web UI");

				// File not found
				processor.WriteErrorHeader();
				return;
			}

			if (uri.UriParts.Count == 0)
			{
				// No path, so return the home page
				path += Path.DirectorySeparatorChar + "index.html";

				// Ensure theme contains an index
				if (!File.Exists(path))
				{
					logger.Error("Theme '" + Injection.Kernel.Get<IServerSettings>().Theme + "' missing required file index.html");

					// File not found
					processor.WriteErrorHeader();
					return;
				}
			}
			else
			{
				// Iterate UriParts to send web pages
				for (int i = 0; i < uri.UriParts.Count; i++)
				{
					string pathPart = uri.UriParts[i];
					if (pathPart.Length > 0 && pathPart[0] == '.')
					{
						// Do not return hidden files/folders
						processor.WriteErrorHeader();
						return;
					}
					else
					{
						path += Path.DirectorySeparatorChar + uri.UriParts[i];
					}
				}
			}

			// Make sure the file exists
			if (!File.Exists(path))
			{
				logger.IfInfo("File does not exist: " + path);

				// File not found
				processor.WriteErrorHeader();
				return;
			}

			// Serve up files inside html directory
			FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
			int startOffset = 0;

			// Handle the Range header to start from later in the file
			if (processor.HttpHeaders.ContainsKey("Range"))
			{
				string range = (string)processor.HttpHeaders["Range"];
				string start = range.Split(new char[]{'-', '='})[1];
				logger.IfInfo("Connection retried.  Resuming from " + start);
				startOffset = Convert.ToInt32(start);
			}

			long length = file.Length - startOffset;

			processor.WriteFile(file, startOffset, length, HttpHeader.MimeTypeForExtension(Path.GetExtension(path)), null, true, new FileInfo(path).LastWriteTimeUtc);
			file.Close();
		}
	}
}
