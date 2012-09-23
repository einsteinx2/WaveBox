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
			Console.WriteLine("Stream handler called");

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
						Console.WriteLine("streaming a video, filename " + item.FileName);
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
						uint quality = (uint)TranscodeQuality.Medium; // Default to medium
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
						if (itemType == ItemType.Song)
						{
							Transcoder = TranscodeManager.Instance.TranscodeSong(item, type, (uint)quality);
						}
						else
						{
							// Check to see if the width, height, and maintainAspect options were used
							uint? width = null;
							if (Uri.Parameters.ContainsKey("width"))
							{
								uint widthTemp;
								width = UInt32.TryParse(Uri.Parameters["width"], out widthTemp) ? (uint?)widthTemp : null;
							}

							uint? height;
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

							Transcoder = TranscodeManager.Instance.TranscodeVideo(item, type, (uint)quality, width, height, maintainAspect);
						}

						if ((object)Transcoder == null) 
						{
							// Stream the original file
							file = item.File;
							length = file.Length;
						}
						else
						{
							length = (long)Transcoder.EstimatedOutputSize;
							
							// Wait up 5 seconds for file to appear
							for (int i = 0; i < 20; i++)
							{
								Console.WriteLine("[STREAM API] Checking if file exists");
								if (File.Exists(Transcoder.OutputPath))
								{
									/*long size = new FileInfo(Transcoder.OutputPath).Length;
									if (size > 50000)
									{
										Console.WriteLine("file size " + size + " starting transfer");
										break;
									}*/
									break;
								}
								
								Thread.Sleep(250);
							}
							
							if (File.Exists(Transcoder.OutputPath))
							{ 
								file = new FileStream(Transcoder.OutputPath, FileMode.Open, FileAccess.Read);
							}
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
						Processor.Transcoder = Transcoder;
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
