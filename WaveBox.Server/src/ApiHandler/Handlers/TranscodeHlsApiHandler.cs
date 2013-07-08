using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.Model;
using WaveBox.Server.Extensions;
using WaveBox.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Transcoding;

namespace WaveBox.ApiHandler.Handlers
{
	public class TranscodeHlsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for TranscodeHlsApiHandler
		/// </summary>
		public TranscodeHlsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process performs a HLS transcode on a media item
		/// </summary>
		public void Process()
		{
			if (logger.IsInfoEnabled) logger.Info("Starting HLS transcoding sequence");

			// Try to get the media item id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			if (!success)
			{
				string json = JsonConvert.SerializeObject(new TranscodeHlsResponse("Missing required parameter 'id'"), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				Processor.WriteJson(json);
			}

			try
			{
				// Get the media item associated with this id
				ItemType itemType = Item.ItemTypeForItemId(id);
				IMediaItem item = null;
				if (itemType == ItemType.Song)
				{
					item = new Song.Factory().CreateSong(id);
					if (logger.IsInfoEnabled) logger.Info("HLS transcoding for songs not currently supported");

					// CURRENTLY DO NOT SUPPORT HLS STREAMING FOR SONGS
					return;
				}
				else if (itemType == ItemType.Video)
				{
					item = new Video.Factory().CreateVideo(id);
					if (logger.IsInfoEnabled) logger.Info("Preparing video stream: " + item.FileName);
				}

				// Return an error if none exists
				if ((item == null) || (!File.Exists(item.FilePath())))
				{
					string json = JsonConvert.SerializeObject(new TranscodeHlsResponse("No media item exists with ID: " + id), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
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
				if (logger.IsInfoEnabled) logger.Info("Successfully HLS transcoded file!");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		/// <summary>
		/// Generates multiple item playlist
		/// <summary>
		private string GenerateMultiPlaylist(IMediaItem item, string[] transQualities)
		{
			// Ensure duration is set
			if ((object)item.Duration == null)
			{
				return null;
			}

			// Grab URI parameters
			string s = Uri.Parameters["s"];
			string id = Uri.Parameters["id"];
			string width = Uri.Parameters.ContainsKey("width") ? Uri.Parameters["width"] : null;
			string height = Uri.Parameters.ContainsKey("height") ? Uri.Parameters["height"] : null;

			// Create new string, write M3U header
			StringBuilder builder = new StringBuilder();

			builder.AppendLine("#EXTM3U");

			// Iterate all transcode qualities
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

				// Append information about this transcode to the playlist
				builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + (bitrate * 1000));
				builder.Append("transcodehls?s=" + s + "&id=" + id + "&transQuality=" + bitrate);
				
				// Add the optional parameters
				if ((object)width != null)
				{
					builder.Append("&width=" + width);
				}
				if ((object)height != null)
				{
					builder.Append("&height=" + height);
				}
				
				builder.AppendLine();
			}

			// Return the completed string
			return builder.ToString();
		}

		/// <summary>
		/// Generate playlist for a single item
		/// </summary>
		private string GeneratePlaylist(IMediaItem item, string transQuality)
		{
			// If duration not set, null!
			if ((object)item.Duration == null)
			{
				return null;
			}

			// Set default parameters from URL
			string s = Uri.Parameters["s"];
			string id = Uri.Parameters["id"];
			string width = Uri.Parameters.ContainsKey("width") ? Uri.Parameters["width"] : null;
			string height = Uri.Parameters.ContainsKey("height") ? Uri.Parameters["height"] : null;

			// Begin creating M3U playlist
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
				{
					builder.Append("&width=" + width);
				}
				if ((object)height != null)
				{
					builder.Append("&height=" + height);
				}

				builder.AppendLine();
				offset += seconds;
			}

			// Finalize file
			builder.AppendLine("#EXT-X-ENDLIST");

			return builder.ToString();
		}

		private class TranscodeHlsResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			public TranscodeHlsResponse(string error)
			{
				Error = error;
			}
		}
	}
}
