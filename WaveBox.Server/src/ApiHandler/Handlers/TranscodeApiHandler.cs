using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.TcpServer.Http;
using WaveBox.Transcoding;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	public class TranscodeApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private ITranscoder Transcoder { get; set; }

		/// <summary>
		/// Constructor for TranscodeApiHandler
		/// </summary>
		public TranscodeApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}
		
		/// <summary>
		/// Process handles the initialization of the file transcoding sequence
		/// <summary>
		public void Process()
		{
			if (logger.IsInfoEnabled) logger.Info("Starting file transcoding sequence");
			
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
					// Set up default transcoding parameters
					ItemType itemType = Item.ItemTypeForItemId(id);
					IMediaItem item = null;
					TranscodeType transType = TranscodeType.MP3;
					bool isDirect = false;
					Stream stream = null;
					int startOffset = 0;
					long length = 0;
					bool estimateContentLength = false;

					// Optionally estimate content length
					if (Uri.Parameters.ContainsKey("estimateContentLength"))
					{
						estimateContentLength = Utility.IsTrue(Uri.Parameters["estimateContentLength"]);
					}

					// Get the media item associated with this id
					if (itemType == ItemType.Song)
					{
						item = new Song.Factory().CreateSong(id);
						if (logger.IsInfoEnabled) logger.Info("Preparing audio transcode: " + item.FileName);
						
						// Default to MP3 transcoding
						transType = TranscodeType.MP3;
					}
					else if (itemType == ItemType.Video)
					{
						item = new Video.Factory().CreateVideo(id);
						if (logger.IsInfoEnabled) logger.Info("Preparing video transcode: " + item.FileName);

						// Default to h.264 transcoding
						transType = TranscodeType.X264;
					}
					
					// Return an error if no item exists
					if ((item == null) || (!File.Exists(item.FilePath)))
					{
						string json = JsonConvert.SerializeObject(new TranscodeResponse("No media item exists with ID: " + id), Settings.JsonFormatting);
						Processor.WriteJson(json);
						return;
					}

					// Optionally add isDirect parameter
					if (Uri.Parameters.ContainsKey("isDirect"))
					{
						isDirect = Utility.IsTrue(Uri.Parameters["isDirect"]);
					}

					// Handle the Range header to start from later in the file
					if (Processor.HttpHeaders.ContainsKey("Range"))
					{
						string range = (string)Processor.HttpHeaders["Range"];
						string start = range.Split(new char[]{'-', '='})[1];
						if (logger.IsInfoEnabled) logger.Info("Connection retried.  Resuming from " + start);

						if (isDirect)
						{
							// This is a direct transfer with no file buffer, so treat a Range request as if it
							// were start offset, unless an offsetSeconds was specified
							if (Uri.Parameters.ContainsKey("offsetSeconds"))
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
						}

					}

					// Get the transcoding type if specified
					if (Uri.Parameters.ContainsKey("transType"))
					{
						// Parse transcoding type
						TranscodeType transTypeTemp;
						if (Enum.TryParse<TranscodeType>(Uri.Parameters["transType"], true, out transTypeTemp))
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
					if (Uri.Parameters.ContainsKey("transQuality"))
					{
						string qualityString = Uri.Parameters["transQuality"];
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
						Transcoder = TranscodeManager.Instance.TranscodeSong(item, transType, (uint)quality, isDirect, 0, (uint)item.Duration);
					}
					else
					{
						// Video transcoding is just a bit more complicated.
						// Check to see if the width, height, and maintainAspect options were used
						uint? width = null;
						if (Uri.Parameters.ContainsKey("width"))
						{
							uint widthTemp;
							width = UInt32.TryParse(Uri.Parameters["width"], out widthTemp) ? (uint?)widthTemp : null;
						}

						uint? height = 0;
						if (Uri.Parameters.ContainsKey("height"))
						{
							uint heightTemp;
							height = UInt32.TryParse(Uri.Parameters["height"], out heightTemp) ? (uint?)heightTemp : null;
						}

						bool maintainAspect = true;
						if (Uri.Parameters.ContainsKey("maintainAspect"))
						{
							if (!Boolean.TryParse(Uri.Parameters["maintainAspect"], out maintainAspect))
							{
								maintainAspect = true;
							}
						}

						// Check for offset seconds and length seconds parameters
						uint offsetSeconds = 0;
						if (Uri.Parameters.ContainsKey("offsetSeconds"))
						{
							UInt32.TryParse(Uri.Parameters["offsetSeconds"], out offsetSeconds);
						}

						uint lengthSeconds = 0;
						if (Uri.Parameters.ContainsKey("lengthSeconds"))
						{
							UInt32.TryParse(Uri.Parameters["lengthSeconds"], out lengthSeconds);
						}

						// Either stream the rest of the file, or the duration specified
						lengthSeconds = lengthSeconds == 0 ? (uint)item.Duration - offsetSeconds : lengthSeconds;
						
						// Begin video transcoding
						Transcoder = TranscodeManager.Instance.TranscodeVideo(item, transType, quality, isDirect, width, height, maintainAspect, offsetSeconds, lengthSeconds);
					}
					
					// If a transcoder was generated...
					if ((object)Transcoder != null)
					{
						length = (long)Transcoder.EstimatedOutputSize;

						// Wait up 5 seconds for file or basestream to appear
						for (int i = 0; i < 20; i++)
						{
							if (Transcoder.IsDirect)
							{
								if (logger.IsInfoEnabled) logger.Info("Checking if base stream exists");
								if ((object)Transcoder.TranscodeProcess != null && (object)Transcoder.TranscodeProcess.StandardOutput.BaseStream != null)
								{
									// The base stream exists, so the transcoding process has started
									if (logger.IsInfoEnabled) logger.Info("Base stream exists, so start the transfer");
									stream = Transcoder.TranscodeProcess.StandardOutput.BaseStream;
									break;
								}
							}
							else
							{
								if (logger.IsInfoEnabled) logger.Info("Checking if file exists (" + Transcoder.OutputPath + ")");
								if (File.Exists(Transcoder.OutputPath))
								{
									// The file exists, so the transcoding process has started
									stream = new FileStream(Transcoder.OutputPath, FileMode.Open, FileAccess.Read);
									break;
								}
							}
							Thread.Sleep(250);
						}
					}

					// Send the file if either there is no transcoder and the original file exists OR
					// it's a direct transcoder and the base stream exists OR
					// it's a file transcoder and the transcoded file exists
					if ((object)Transcoder == null && File.Exists(item.FilePath) || 
						(Transcoder.IsDirect && (object)stream != null) ||
						(!Transcoder.IsDirect && File.Exists(Transcoder.OutputPath)))
					{
						if (logger.IsInfoEnabled) logger.Info("Sending direct stream");
						string mimeType = (object)Transcoder == null ? item.FileType.MimeType() : Transcoder.MimeType;
						Processor.Transcoder = Transcoder;

						if (Uri.Parameters.ContainsKey("offsetSeconds"))
						{
							if (logger.IsInfoEnabled) logger.Info("ApiHandlerFactory writing file at offsetSeconds " + Uri.Parameters["offsetSeconds"]);
						}

						// Direct write file
						Processor.WriteFile(stream, startOffset, length, mimeType, null, estimateContentLength);
						stream.Close();
						if (logger.IsInfoEnabled) logger.Info("Successfully sent direct stream");

						if (Uri.Parameters.ContainsKey("offsetSeconds"))
						{
							if (logger.IsInfoEnabled) logger.Info("ApiHandlerFactory DONE writing file at offsetSeconds " + Uri.Parameters["offsetSeconds"]);
						}
					}
					else
					{
						Processor.WriteErrorHeader();
					}

					// Spin off a thread to consume the transcoder in 30 seconds.
					Thread consume = new Thread(() => TranscodeManager.Instance.ConsumedTranscode(Transcoder));
					consume.Start();
					//TranscodeManager.Instance.ConsumedTranscode(Transcoder);
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
			}
			else
			{
				string json = JsonConvert.SerializeObject(new TranscodeResponse("Missing required parameter 'id'"), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
		}

		private class TranscodeResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			public TranscodeResponse(string error)
			{
				Error = error;
			}
		}
	}
}
