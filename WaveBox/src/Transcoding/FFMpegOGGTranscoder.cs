using System;
using WaveBox.Model;

namespace WaveBox.Transcoding
{
	public class FFMpegOGGTranscoder : AbstractTranscoder
	{
		//private static Logger logger = LogManager.GetCurrentClassLogger();

		public override TranscodeType Type { get { return TranscodeType.OGG; } }

		public override string Codec { get { return "libvorbis"; } }

		public override string Command { get { return "ffmpeg"; } }

		public override string OutputExtension { get { return "ogg"; } }

		public override string MimeType { get { return "audio/ogg"; } }

		public FFMpegOGGTranscoder(IMediaItem item, uint quality, bool isDirect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, offsetSeconds, lengthSeconds)
		{
			
		}

		public override string Arguments
		{
			get 
			{ 
				string options = null;
				switch (Quality)
				{
					case (uint)TranscodeQuality.Low:
						// VBR - Q0 quality (~64 kbps)
						options = FFMpegOptionsWith(0);
						break;
					case (uint)TranscodeQuality.Medium:
						// VBR - Q4 quality (~128 kbps)
						options = FFMpegOptionsWith(5);
						break;
					case (uint)TranscodeQuality.High:
						// VBR - Q5 quality (~160 kbps)
						options = FFMpegOptionsWith(2);
						break;
					case (uint)TranscodeQuality.Extreme:
						// VBR - Q6 quality (~192 kbps)
						options = FFMpegOptionsWith(0);
						break;
					default:
						options = FFMpegOptionsWith(Quality);
						break;
				}
				return options;
			}
		}

		public override uint? EstimatedBitrate
		{
			get 
			{ 
				uint? bitrate = null;
				switch (Quality)
				{
					case (uint)TranscodeQuality.Low: 
						bitrate = 64;
						break;
					case (uint)TranscodeQuality.Medium: 
						bitrate = 128;		
						break;
					case (uint)TranscodeQuality.High: 
						bitrate = 160;
						break;
					case (uint)TranscodeQuality.Extreme: 
						bitrate = 192;
						break;
					default: 
						bitrate = Quality;
						break;
				}
				return bitrate;
			}
		}

		private string FFMpegOptionsWith(uint quality)
		{
			if (quality > 12)
			{
				return "-loglevel quiet -i \"" + Item.FilePath + "\" -acodec " + Codec + " -ab " + quality + " " + OutputPath;
			}
			else
			{
				return "-loglevel quiet -i \"" + Item.FilePath + "\" -acodec " + Codec + " -aq " + quality + " " + OutputPath;
			}
		}
	}
}

