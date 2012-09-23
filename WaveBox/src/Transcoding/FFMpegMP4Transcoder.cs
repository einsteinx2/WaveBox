using System;
using WaveBox.DataModel.Model;

namespace WaveBox.Transcoding
{
	/*public class FFMpegMP4Transcoder : AbstractTranscoder
	{
		public override TranscodeType Type { get { return TranscodeType.MP4; } }

		public override string Command { get { return "ffmpeg"; } }
		
		public FFMpegMP4Transcoder(IMediaItem item, uint quality) : base(item, quality)
		{
			
		}
		
		public override string Arguments
		{
			get 
			{ 
				string acodec = "aac";
				string vcodec = "mpeg4";
				string options = null;
				switch (Quality)
				{
					case (uint)TranscodeQuality.Low:
						// VBR - V9 quality (~64 kbps)
						options = FFMpegOptionsWith(acodec, 9);
						break;
					case (uint)TranscodeQuality.Medium:
						// VBR - V5 quality (~128 kbps)
						options = FFMpegOptionsWith(codec, 5);
						break;
					case (uint)TranscodeQuality.High:
						// VBR - V2 quality (~192 kbps)
						options = FFMpegOptionsWith(codec, 2);
						break;
					case (uint)TranscodeQuality.Extreme:
						// VBR - V0 quality (~224 kbps)
						options = FFMpegOptionsWith(codec, 0);
						break;
					default:
						
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
				}
				return bitrate;
			}
		}
		
		public override string OutputExtension
		{
			get
			{
				return "mp4";
			}
		}

		private string GenerateArguments(int abitrate, int vbitrate, int? width, int? height, bool? maintainAspect)
		{
			return "-i \"" + Item.FilePath + "\" input -acodec aac -ab " + abitrate + " -vcodec " + vcodec + " -b " + vbitrate + " -mbd 2 -flags +4mv+trell -aic 2 -cmp 2 -subcmp 2 -s 320x180 -title X final_video.mp4";
		}
	}*/
}

