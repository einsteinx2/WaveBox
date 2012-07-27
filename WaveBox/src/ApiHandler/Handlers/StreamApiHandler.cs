using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using WaveBox.Transcoding;
using System.IO;
using WaveBox.HttpServer;


namespace WaveBox.ApiHandler.Handlers
{
	class StreamApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private ITranscoder Transcoder { get; set; }

		public StreamApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			if (Uri.UriPart(2) != null)
			{
				try
				{
					// This will really be a mediaitem object, but it's just a song for now.
					MediaItem item = new Song(Convert.ToInt32(Uri.UriPart(2)));
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
							quality = (TranscodeQuality)Enum.Parse(typeof(TranscodeQuality), Uri.Parameters["transQuality"], true);

						// Create the transcoder
						Transcoder = TranscodeManager.Instance.CreateTranscoder(item, type, quality);
						file = new FileStream(Transcoder.OutputPath, FileMode.Open, FileAccess.Read);
						length = (long)Transcoder.EstimatedOutputSize;
					}
					else
					{
						// Stream the original file
						file = item.File();
						length = file.Length;
					}

					// Send the file
					WaveBoxHttpServer.sendFile(Processor, file, startOffset, length);

					// Consume the transcode (if transcoded)
					TranscodeManager.Instance.ConsumedTranscode(Transcoder);
				}
				catch (Exception e)
				{
					Console.WriteLine("[STREAMAPI] ERROR: " + e.ToString());
				}
			}
		}
	}
}
