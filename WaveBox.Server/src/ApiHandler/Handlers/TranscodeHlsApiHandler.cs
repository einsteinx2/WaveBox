using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Server.Extensions;
using WaveBox.Service.Services.Http;
using WaveBox.Static;
using WaveBox.Transcoding;

namespace WaveBox.ApiHandler.Handlers
{
	public class TranscodeHlsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "transcodehls"; } }

		// API handler is read-only, so no permissions checks needed
		public bool CheckPermission(User user, string action)
		{
			return true;
		}

		/// <summary>
		/// Process performs a HLS transcode on a media item
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Verify ID received
			if (uri.Id == null)
			{
				processor.WriteJson(new TranscodeHlsResponse("Missing required parameter 'id'"));
				return;
			}

			try
			{
				// Get the media item associated with this id
				ItemType itemType = Injection.Kernel.Get<IItemRepository>().ItemTypeForItemId((int)uri.Id);
				IMediaItem item = null;
				if (itemType == ItemType.Song)
				{
					item = Injection.Kernel.Get<ISongRepository>().SongForId((int)uri.Id);
					logger.IfInfo("HLS transcoding for songs not currently supported");

					// CURRENTLY DO NOT SUPPORT HLS STREAMING FOR SONGS
					return;
				}
				else if (itemType == ItemType.Video)
				{
					item = Injection.Kernel.Get<IVideoRepository>().VideoForId((int)uri.Id);
					logger.IfInfo("Preparing video stream: " + item.FileName);
				}

				// Return an error if none exists
				if ((item == null) || (!File.Exists(item.FilePath())))
				{
					processor.WriteJson(new TranscodeHlsResponse("No media item exists with ID: " + (int)uri.Id));
					return;
				}

				// Generate the playlist file
				string response = null;
				string[] transQualities = uri.Parameters.ContainsKey("transQuality") ? uri.Parameters["transQuality"].Split(',') : new string[] {"Medium"};
				if (transQualities.Length == 1)
				{
					// This is a single playlist
					response = this.GeneratePlaylist(item, transQualities[0], uri);
				}
				else
				{
					// This is a multi playlist
					response = this.GenerateMultiPlaylist(item, transQualities, uri);
				}

				processor.WriteText(response, "application/x-mpegURL");
				logger.IfInfo("Successfully HLS transcoded file!");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		/// <summary>
		/// Generates multiple item playlist
		/// <summary>
		private string GenerateMultiPlaylist(IMediaItem item, string[] transQualities, UriWrapper uri)
		{
			// Ensure duration is set
			if ((object)item.Duration == null)
			{
				return null;
			}

			// Grab URI parameters
			string s = uri.Parameters["s"];
			string id = uri.Parameters["id"];
			string width = uri.Parameters.ContainsKey("width") ? uri.Parameters["width"] : null;
			string height = uri.Parameters.ContainsKey("height") ? uri.Parameters["height"] : null;

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
		private string GeneratePlaylist(IMediaItem item, string transQuality, UriWrapper uri)
		{
			// If duration not set, null!
			if ((object)item.Duration == null)
			{
				return null;
			}

			// Set default parameters from URL
			string s = uri.Parameters["s"];
			string id = uri.Parameters["id"];
			string width = uri.Parameters.ContainsKey("width") ? uri.Parameters["width"] : null;
			string height = uri.Parameters.ContainsKey("height") ? uri.Parameters["height"] : null;

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
	}
}
