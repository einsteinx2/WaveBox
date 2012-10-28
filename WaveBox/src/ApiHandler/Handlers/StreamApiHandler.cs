using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using WaveBox.Transcoding;
using WaveBox.DataModel.Singletons;
using System.IO;
using WaveBox.Http;
using System.Threading;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	class StreamApiHandler : IApiHandler
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public StreamApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			logger.Info("Stream handler called");

			// Try to get the media item id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			if (success)
			{
				try
				{
					// Get the media item associated with this id
					ItemType itemType = Item.ItemTypeForItemId(id);
					IMediaItem item = null;
					if (itemType == ItemType.Song)
					{
						item = new Song(id);
					}
					else if (itemType == ItemType.Video)
					{
						item = new Video(id);
						logger.Info("streaming a video, filename " + item.FileName);
					}

					// Return an error if none exists
					if (item == null || !File.Exists(item.FilePath))
					{
						new ErrorApiHandler(Uri, Processor, "No media item exists for id: " + id).Process();
						return;
					}

					Stream stream = item.File;
					long length = stream.Length;
					int startOffset = 0;

					// Handle the Range header to start from later in the file
					if (Processor.HttpHeaders.ContainsKey("Range"))
					{
						string range = (string)Processor.HttpHeaders["Range"];
						string start = range.Split(new char[]{'-', '='})[1];
						logger.Info("[SENDFILE] Connection retried.  Resuming from {0}", start);
						startOffset = Convert.ToInt32(start);
					}

                    var dict = new Dictionary<string, string>();
                    var lmt = HttpProcessor.DateTimeToLastMod(new FileInfo(item.FilePath).LastWriteTimeUtc);
                    dict.Add("Last-Modified", lmt);

					// Send the file
                    Processor.WriteFile(stream, startOffset, length, item.FileType.MimeType(), dict, true);
				}
				catch(Exception e)
				{
					logger.Error("[STREAMAPI] ERROR: " + e);
				}
			}
			else
			{
				new ErrorApiHandler(Uri, Processor, "Missing parameter: \"id\"").Process();
			}
		}
	}
}
