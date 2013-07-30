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
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using WaveBox.Server.Extensions;
using WaveBox.Service;
using WaveBox.Service.Services.Http;
using WaveBox.Service.Services;
using WaveBox.Static;
using WaveBox.Transcoding;

namespace WaveBox.ApiHandler.Handlers
{
	public class TranscodeApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "transcode"; } set { } }

		/// <summary>
		/// Process handles the initialization of the file transcoding sequence
		/// <summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Get TranscodeService instance
			TranscodeService transcodeService = (TranscodeService)ServiceManager.GetInstance("transcode");

			// Ensure transcode service is ready
			if ((object)transcodeService == null)
			{
				string json = JsonConvert.SerializeObject(new TranscodeResponse("TranscodeService is not running!"), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				processor.WriteJson(json);
				return;
			}

			// Create transcoder
			ITranscoder transcoder = null;

			if (logger.IsInfoEnabled) logger.Info("Starting file transcoding sequence");

			// Try to get the media item id
			bool success = false;
			int id = 0;
			if (uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(uri.Parameters["id"], out id);
			}

			if (!success)
			{
				string json = JsonConvert.SerializeObject(new TranscodeResponse("Missing required parameter 'id'"), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				processor.WriteJson(json);
			}

			try
			{
				// Set up default transcoding parameters
				ItemType itemType = Injection.Kernel.Get<IItemRepository>().ItemTypeForItemId(id);
				IMediaItem item = null;
				TranscodeType transType = TranscodeType.MP3;
				bool isDirect = false;
				Stream stream = null;
				int startOffset = 0;
				long? limitToSize = null;
				long length = 0;
				bool estimateContentLength = false;

				// Optionally estimate content length
				if (uri.Parameters.ContainsKey("estimateContentLength"))
				{
					estimateContentLength = uri.Parameters["estimateContentLength"].IsTrue();
				}

				// Get the media item associated with this id
				if (itemType == ItemType.Song)
				{
					item = Injection.Kernel.Get<ISongRepository>().SongForId(id);
					if (logger.IsInfoEnabled) logger.Info("Preparing audio transcode: " + item.FileName);

					// Default to MP3 transcoding
					transType = TranscodeType.MP3;
				}
				else if (itemType == ItemType.Video)
				{
					item = Injection.Kernel.Get<IVideoRepository>().VideoForId(id);
					if (logger.IsInfoEnabled) logger.Info("Preparing video transcode: " + item.FileName);

					// Default to h.264 transcoding
					transType = TranscodeType.X264;
				}

				// Return an error if no item exists
				if ((item == null) || (!File.Exists(item.FilePath())))
				{
					string json = JsonConvert.SerializeObject(new TranscodeResponse("No media item exists with ID: " + id), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					processor.WriteJson(json);
					return;
				}

				// Optionally add isDirect parameter
				if (uri.Parameters.ContainsKey("isDirect"))
				{
					isDirect = uri.Parameters["isDirect"].IsTrue();
				}

				// Handle the Range header to start from later in the file
				if (processor.HttpHeaders.ContainsKey("Range"))
				{
					string range = (string)processor.HttpHeaders["Range"];
					var split = range.Split(new char[]{'-', '='});
					string start = split[1];
					string end = split.Length > 2 ? split[2] : null;
					if (logger.IsInfoEnabled) logger.Info("Range header: " + range + "  Resuming from " + start + " end: " + end);

					if (isDirect)
					{
						// TODO: Actually implement this lol
						// This is a direct transfer with no file buffer, so treat a Range request as if it
						// were start offset, unless an offsetSeconds was specified
						if (uri.Parameters.ContainsKey("offsetSeconds"))
						{

						}
						else
						{

						}
					}
					else
					{
						// This is a file request so use the range header to specify where in the file to return
						startOffset = Convert.ToInt32(start);
						if (!ReferenceEquals(end, null) && end.Length > 0)
						{
							limitToSize = (Convert.ToInt64(end) + 1) - startOffset;
						}
					}
				}

				// Get the transcoding type if specified
				if (uri.Parameters.ContainsKey("transType"))
				{
					// Parse transcoding type
					TranscodeType transTypeTemp;
					if (Enum.TryParse<TranscodeType>(uri.Parameters["transType"], true, out transTypeTemp))
					{
						// Verify this is a valid transcode type for this item type
						if (transTypeTemp.IsValidForItemType(item.ItemType))
						{
							// It is, so use it
							transType = transTypeTemp;
						}
					}
				}

				// Get the quality, default to medium
				uint quality = (uint)TranscodeQuality.Medium;
				if (uri.Parameters.ContainsKey("transQuality"))
				{
					string qualityString = uri.Parameters["transQuality"];
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
				}

				// Create the transcoder
				if (item.ItemType == ItemType.Song)
				{
					// Begin transcoding song
					transcoder = transcodeService.TranscodeSong(item, transType, (uint)quality, isDirect, 0, (uint)item.Duration);
				}
				else
				{
					// Video transcoding is just a bit more complicated.
					// Check to see if the width, height, and maintainAspect options were used
					uint? width = null;
					if (uri.Parameters.ContainsKey("width"))
					{
						uint widthTemp;
						width = UInt32.TryParse(uri.Parameters["width"], out widthTemp) ? (uint?)widthTemp : null;
					}

					uint? height = 0;
					if (uri.Parameters.ContainsKey("height"))
					{
						uint heightTemp;
						height = UInt32.TryParse(uri.Parameters["height"], out heightTemp) ? (uint?)heightTemp : null;
					}

					bool maintainAspect = true;
					if (uri.Parameters.ContainsKey("maintainAspect"))
					{
						if (!Boolean.TryParse(uri.Parameters["maintainAspect"], out maintainAspect))
						{
							maintainAspect = true;
						}
					}

					// Check for offset seconds and length seconds parameters
					uint offsetSeconds = 0;
					if (uri.Parameters.ContainsKey("offsetSeconds"))
					{
						UInt32.TryParse(uri.Parameters["offsetSeconds"], out offsetSeconds);
					}

					uint lengthSeconds = 0;
					if (uri.Parameters.ContainsKey("lengthSeconds"))
					{
						UInt32.TryParse(uri.Parameters["lengthSeconds"], out lengthSeconds);
					}

					// Either stream the rest of the file, or the duration specified
					lengthSeconds = lengthSeconds == 0 ? (uint)item.Duration - offsetSeconds : lengthSeconds;

					// Begin video transcoding
					transcoder = transcodeService.TranscodeVideo(item, transType, quality, isDirect, width, height, maintainAspect, offsetSeconds, lengthSeconds);
				}

				// If a transcoder was generated...
				if ((object)transcoder != null)
				{
					length = (long)transcoder.EstimatedOutputSize;

					// Wait up 5 seconds for file or basestream to appear
					for (int i = 0; i < 20; i++)
					{
						if (transcoder.IsDirect)
						{
							if (logger.IsInfoEnabled) logger.Info("Checking if base stream exists");
							if ((object)transcoder.TranscodeProcess != null && (object)transcoder.TranscodeProcess.StandardOutput.BaseStream != null)
							{
								// The base stream exists, so the transcoding process has started
								if (logger.IsInfoEnabled) logger.Info("Base stream exists, so start the transfer");
								stream = transcoder.TranscodeProcess.StandardOutput.BaseStream;
								break;
							}
						}
						else
						{
							if (logger.IsInfoEnabled) logger.Info("Checking if file exists (" + transcoder.OutputPath + ")");
							if (File.Exists(transcoder.OutputPath))
							{
								// The file exists, so the transcoding process has started
								stream = new FileStream(transcoder.OutputPath, FileMode.Open, FileAccess.Read);
								break;
							}
						}
						Thread.Sleep(250);
					}
				}

				// Send the file if either there is no transcoder and the original file exists OR
				// it's a direct transcoder and the base stream exists OR
				// it's a file transcoder and the transcoded file exists
				if ((object)transcoder == null && File.Exists(item.FilePath()) ||
					(transcoder.IsDirect && (object)stream != null) ||
					(!transcoder.IsDirect && File.Exists(transcoder.OutputPath)))
				{
					if (logger.IsInfoEnabled) logger.Info("Sending direct stream");
					string mimeType = (object)transcoder == null ? item.FileType.MimeType() : transcoder.MimeType;
					processor.Transcoder = transcoder;

					if (uri.Parameters.ContainsKey("offsetSeconds"))
					{
						if (logger.IsInfoEnabled) logger.Info("ApiHandlerFactory writing file at offsetSeconds " + uri.Parameters["offsetSeconds"]);
					}

					DateTime lastModified = transcoder.IsDirect ? DateTime.UtcNow : new FileInfo(transcoder.OutputPath).LastWriteTimeUtc;

					// Direct write file
					processor.WriteFile(stream, startOffset, length, mimeType, null, estimateContentLength, lastModified, limitToSize);
					stream.Close();
					if (logger.IsInfoEnabled) logger.Info("Successfully sent direct stream");

					if (uri.Parameters.ContainsKey("offsetSeconds"))
					{
						if (logger.IsInfoEnabled) logger.Info("ApiHandlerFactory DONE writing file at offsetSeconds " + uri.Parameters["offsetSeconds"]);
					}
				}
				else
				{
					processor.WriteErrorHeader();
				}

				// Spin off a thread to consume the transcoder in 30 seconds.
				Thread consume = new Thread(() => transcodeService.ConsumedTranscode(transcoder));
				consume.Start();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
