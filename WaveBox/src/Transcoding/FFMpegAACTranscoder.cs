using System;
using WaveBox.DataModel.Model;
using NLog;

namespace WaveBox.Transcoding
{
	public class FFMpegAACTranscoder : AbstractTranscoder
	{
		public override TranscodeType Type { get { return TranscodeType.MP3; } }

		public override string Codec { get { return "libfaac"; } }

		public override string Command { get { return "ffmpeg"; } }

		public override string OutputExtension { get { return "mp4"; } }

		public override string MimeType { get { return "audio/mp4"; } }

		public FFMpegAACTranscoder(IMediaItem item, uint quality, bool isDirect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, offsetSeconds, lengthSeconds)
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
		                // VBR - 70 quality (~64 kbps)
		                options = FFMpegOptionsWith(70);
						break;
					case (uint)TranscodeQuality.Medium:
		                // VBR - 100 quality (~128 kbps)
		                options = FFMpegOptionsWith(100);
						break;
					case (uint)TranscodeQuality.High:
		                // VBR - V2 quality (~192 kbps)
		                options = FFMpegOptionsWith(120);
						break;
					case (uint)TranscodeQuality.Extreme:
		                // VBR - V0 quality (~224 kbps)
		                options = FFMpegOptionsWith(130);
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
				return "-loglevel quiet -i \"" + Item.FilePath + "\" -acodec " + Codec + " -ab " + quality + " " + OutputPath;
			else
				return "-loglevel quiet -i \"" + Item.FilePath + "\" -acodec " + Codec + " -aq " + quality + " " + OutputPath;
		}
	}
}

