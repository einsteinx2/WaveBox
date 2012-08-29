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


namespace WaveBox.ApiHandler.Handlers
{
	class StreamApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private ITranscoder Transcoder { get; set; }

		public StreamApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
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
					MediaItem item = null;
					if (itemType == ItemType.Song)
					{
						item = new Song(id);
					}

					// Return an error if none exists
					if (item == null)
					{
						new ErrorApiHandler(Uri, Processor, "No media item exists for id: " + id).Process();
						return;
					}

					// This will really be a mediaitem object, but it's just a song for now.
					FileStream file = null;
					int startOffset = 0;
					long length = 0;

					// Handle the Range header to start from later in the file
					if (Processor.HttpHeaders.ContainsKey("Range"))
					{
						string range = (string)Processor.HttpHeaders["Range"];
						string start = range.Split(new char[]{'-', '='})[1];
						Console.WriteLine("[SENDFILE] Connection retried.  Resuming from {0}", start);
						startOffset = Convert.ToInt32(start);
					}

					// Check if we need to enable transcoding
					if (Uri.Parameters.ContainsKey("transType"))
					{
						// Get the type
						TranscodeType type = (TranscodeType)Enum.Parse(typeof(TranscodeType), Uri.Parameters["transType"], true);

						// Get the quality
						TranscodeQuality quality = TranscodeQuality.Medium; // Default to medium
						if (Uri.Parameters.ContainsKey("transQuality"))
						{
							quality = (TranscodeQuality)Enum.Parse(typeof(TranscodeQuality), Uri.Parameters["transQuality"], true);
						}

						// Create the transcoder
						Transcoder = TranscodeManager.Instance.TranscodeItem(item, type, quality);
						length = (long)Transcoder.EstimatedOutputSize;

						// Wait up 5 seconds for file to appear
						for (int i = 0; i < 20; i++)
						{
							Console.WriteLine("[STREAM API] Checking if file exists");
							if (File.Exists(Transcoder.OutputPath))
							{
								break;
							}

							Thread.Sleep(250);
						}

						if (File.Exists(Transcoder.OutputPath))
						{ 
							file = new FileStream(Transcoder.OutputPath, FileMode.Open, FileAccess.Read);
						}
					}
					else
					{
						// Stream the original file
						file = item.File;
						length = file.Length;
					}

					// Send the file
					if (this.Transcoder == null || File.Exists(Transcoder.OutputPath))
					{
						Processor.WriteFile(file, startOffset, length);
					}
					else
					{
						Processor.WriteErrorHeader();
					}

					// Consume the transcode (if transcoded)
					TranscodeManager.Instance.ConsumedTranscode(Transcoder);
				}
				catch(Exception e)
				{
					Console.WriteLine("[STREAMAPI] ERROR: " + e.ToString());
				}
			}
			else
			{
				new ErrorApiHandler(Uri, Processor, "Missing parameter: \"id\"").Process();
			}
		}
	}
}
