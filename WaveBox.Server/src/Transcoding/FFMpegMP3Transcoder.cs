using System;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.Server.Extensions;

namespace WaveBox.Transcoding
{
	public class FFMpegMP3Transcoder : AbstractTranscoder
	{
		public override TranscodeType Type { get { return TranscodeType.MP3; } }

		public override string Codec { get { return "libmp3lame"; } }

		public override string Command { get { return "ffmpeg"; } }

		public override string OutputExtension { get { return "mp3"; } }

		public override string MimeType { get { return "audio/mpeg"; } }

		public FFMpegMP3Transcoder(IMediaItem item, uint quality, bool isDirect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, offsetSeconds, lengthSeconds)
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
						// VBR - V9 quality (~64 kbps)
						options = FFMpegOptionsWith(9);
						break;
					case (uint)TranscodeQuality.Medium:
						// VBR - V5 quality (~128 kbps)
						options = FFMpegOptionsWith(5);
						break;
					case (uint)TranscodeQuality.High:
						// VBR - V2 quality (~192 kbps)
						options = FFMpegOptionsWith(2);
						break;
					case (uint)TranscodeQuality.Extreme:
						// VBR - V0 quality (~224 kbps)
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
						bitrate = 192;
						break;
					case (uint)TranscodeQuality.Extreme: 
						bitrate = 224;
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
				return "-loglevel quiet -i \"" + Item.FilePath() + "\" -acodec " + Codec + " -ab " + (quality * 1024) + " " + OutputPath;
			}
			else
			{
				return "-loglevel quiet -i \"" + Item.FilePath() + "\" -acodec " + Codec + " -aq " + quality + " " + OutputPath;
			}
		}
	}
}

