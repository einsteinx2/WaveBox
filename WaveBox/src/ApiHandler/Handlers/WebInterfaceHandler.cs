using System;
using System.IO;

namespace WaveBox.ApiHandler.Handlers
{
	public class WebInterfaceHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		private const string rootPath = "html";

		public WebInterfaceHandler(UriWrapper uri, IHttpProcessor processor)
		{
			Uri = uri;
			Processor = processor;
		}

		public void Process()
		{
			string path = rootPath;
			if (Uri.UriParts.Count == 0)
			{
				// No path, so return the home page
				path += Path.DirectorySeparatorChar + "index.html";
			}
			else
			{
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

			Console.WriteLine("[WEBINTERFACE] path: " + path);

			// Send the file
			if (File.Exists(path))
			{
				Console.WriteLine("[WEBINTERFACE] serving file at path: " + path);

				// Serve up files inside html directory
				FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
				int startOffset = 0;

				// Handle the Range header to start from later in the file
				if (Processor.HttpHeaders.ContainsKey("Range"))
				{
					string range = (string)Processor.HttpHeaders["Range"];
					string start = range.Split(new char[]{'-', '='})[1];
					Console.WriteLine("[WEBINTERFACE] Connection retried.  Resuming from {0}", start);
					startOffset = Convert.ToInt32(start);
				}

				long length = file.Length - startOffset;

				Processor.WriteFile(file, startOffset, length);
			}
			else
			{
				Console.WriteLine("[WEBINTERFACE] file at path does not exist: " + path);

				// File not found
				Processor.WriteErrorHeader();
			}
		}
	}
}

