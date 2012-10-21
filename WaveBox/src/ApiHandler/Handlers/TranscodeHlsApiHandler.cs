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
	class TranscodeHlsApiHandler : IApiHandler
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		
		public TranscodeHlsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
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
					IMediaItem item = null;
					if (itemType == ItemType.Song)
					{
						item = new Song(id);

						// CURRENTLY DO NOT SUPPORT HLS STREAMING FOR SONGS
						return;
					}
					else if (itemType == ItemType.Video)
					{
						item = new Video(id);
					}
					
					// Return an error if none exists
					if (item == null || !File.Exists(item.FilePath))
					{
						new ErrorApiHandler(Uri, Processor, "No media item exists for id: " + id).Process();
						return;
					}

					// Generate the playlist file
					string response = null;
					string[] transQualities = Uri.Parameters.ContainsKey("transQuality") ? Uri.Parameters["transQuality"].Split(',') : new string[] {"Medium"};
					if (transQualities.Length == 1)
					{
						// This is a single playlist
						response = GeneratePlaylist(item, transQualities[0]);
					}
					else
					{
						// This is a multi playlist
						response = GenerateMultiPlaylist(item, transQualities);
					}
				
					Processor.WriteText(response, "application/x-mpegURL");
				}
				catch(Exception e)
				{
					logger.Info("[STREAMAPI] ERROR: " + e);
				}
			}
			else
			{
				new ErrorApiHandler(Uri, Processor, "Missing parameter: \"id\"").Process();
			}
		}

		private string GenerateMultiPlaylist(IMediaItem item, string[] transQualities)
		{
			if ((object)item.Duration == null)
			{
				return null;
			}

			string s = Uri.Parameters["s"];
			string id = Uri.Parameters["id"];
			string width = Uri.Parameters.ContainsKey("width") ? Uri.Parameters["width"] : null;
			string height = Uri.Parameters.ContainsKey("height") ? Uri.Parameters["height"] : null;

			StringBuilder builder = new StringBuilder();

			builder.AppendLine("#EXTM3U");

			foreach (string qualityString in transQualities)
			{
				// Get the quality, default to medium
				uint quality = (uint)TranscodeQuality.Medium;
				TranscodeQuality qualityEnum;
				uint qualityValue;
				// First try and parse a word enum value
				if (Enum.TryParse<TranscodeQuality>(qualityString, true, out qualityEnum))
				{
					quality = (uint)qualityEnum;
				}
				// Otherwise look for a number to use as bitrate
				else if (UInt32.TryParse(qualityString, out qualityValue))
				{
					quality = qualityValue;
				}
				uint bitrate = VideoTranscoder.DefaultBitrateForQuality(quality);

				builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + (bitrate * 1000));
				builder.Append("transcodehls?s=" + s + "&id=" + id + "&transQuality=" + bitrate);
				
				// Add the optional parameters
				if ((object)width != null)
					builder.Append("&width=" + width);
				if ((object)height != null)
					builder.Append("&height=" + height);
				
				builder.AppendLine();
			}

			return builder.ToString();
		}

		private string GeneratePlaylist(IMediaItem item, string transQuality)
		{
			if ((object)item.Duration == null)
			{
				return null;
			}

			logger.Info("duration: " + item.Duration);

			string s = Uri.Parameters["s"];
			string id = Uri.Parameters["id"];
			string width = Uri.Parameters.ContainsKey("width") ? Uri.Parameters["width"] : null;
			string height = Uri.Parameters.ContainsKey("height") ? Uri.Parameters["height"] : null;

			StringBuilder builder = new StringBuilder();

			builder.AppendLine("#EXTM3U");
			builder.AppendLine("#EXT-X-TARGETDURATION:10");
			builder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

			int offset = 0;
			for (int i = (int)item.Duration; i > 0; i -= 10)
			{
				// Calculate the length of this slice
				int seconds = i < 10 ? i : 10;

				// Add the default line
				builder.AppendLine("#EXTINF:" + seconds + ",");
				builder.Append("transcode?s=" + s + "&id=" + id + "&offsetSeconds=" + offset + "&transQuality=" + transQuality + "&lengthSeconds=" + seconds + "&transType=MPEGTS&isDirect=true");

				// Add the optional parameters
				if ((object)width != null)
					builder.Append("&width=" + width);
				if ((object)height != null)
					builder.Append("&height=" + height);

				builder.AppendLine();
				offset += seconds;
			}

			builder.AppendLine("#EXT-X-ENDLIST");

			return builder.ToString();
		}
	}
}
