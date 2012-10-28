using System;
using System.IO;
using System.Collections.Generic;
using WaveBox.Http;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	public class WebInterfaceHandler : IApiHandler
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

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

			logger.Info("[WEBINTERFACE] path: " + path);

			// Make sure the file exists
			if (File.Exists(path))
			{
                // If it exists, check to see if the headers contains an If-Modified-Since entry
                if(Processor.HttpHeaders.ContainsKey("If-Modified-Since"))
                {
                    logger.Info(Processor.HttpHeaders["If-Modified-Since"]);

                    // Took me a while to figure this out, but even if the time zone in the request is GMT, DateTime.Parse converts it to local time.
                    var ims = DateTime.Parse(Processor.HttpHeaders["If-Modified-Since"].ToString());
                    var lastMod = File.GetLastWriteTime(path);

                    if(ims >= lastMod)
                    {
                        logger.Info("[WEBINTERFACE] File not modified: " + path);
						Processor.WriteNotModifiedHeader();
                        return;
                    }
                }

				logger.Info("[WEBINTERFACE] serving file at path: " + path);

				// Serve up files inside html directory
				FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
				int startOffset = 0;

				// Handle the Range header to start from later in the file
				if (Processor.HttpHeaders.ContainsKey("Range"))
				{
					string range = (string)Processor.HttpHeaders["Range"];
					string start = range.Split(new char[]{'-', '='})[1];
					logger.Info("[WEBINTERFACE] Connection retried.  Resuming from {0}", start);
					startOffset = Convert.ToInt32(start);
				}

				long length = file.Length - startOffset;

                var dict = new Dictionary<string, string>();
                var lwt = HttpProcessor.DateTimeToLastMod(new FileInfo(path).LastWriteTimeUtc);
                dict.Add("Last-Modified", lwt);

				Processor.WriteFile(file, startOffset, length, HttpHeader.MimeTypeForExtension(Path.GetExtension(path)), dict, true);

			}
			else
			{
				logger.Info("[WEBINTERFACE] file at path does not exist: " + path);

				// File not found
				Processor.WriteErrorHeader();
			}
		}
        
        private static int MonthForAbbreviation(string abb)
        {
            switch (abb.ToLower())
            {
	            case "jan": return 1;
	            case "feb": return 2;
	            case "mar": return 3;
	            case "apr": return 4;
	            case "may": return 5;
	            case "jun": return 6;
	            case "jul": return 7;
	            case "aug": return 8;
	            case "sep": return 9;
	            case "oct": return 10;
	            case "nov": return 11;
	            case "dec": return 12;
	            default: return 0;
            }
        }
    }
}

