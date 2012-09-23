using System;
using WaveBox.DataModel.Model;

namespace WaveBox.Transcoding
{
	public class FFMpegMP3Transcoder : AbstractTranscoder
	{
		public override TranscodeType Type { get { return TranscodeType.MP3; } }

		public override string Command { get { return "ffmpeg"; } }

		public override string OutputExtension { get { return "mp3"; } }

		public FFMpegMP3Transcoder(IMediaItem item, uint quality) : base(item, quality)
    	{
        	
    	}

	    public override string Arguments
	    {
			get 
			{ 
				string codec = "libmp3lame";
		        string options = null;
		        switch (Quality)
		        {
		            case (uint)TranscodeQuality.Low:
		                // VBR - V9 quality (~64 kbps)
		                options = FFMpegOptionsWith(codec, 9);
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
						options = FFMpegOptionsWith(codec, Quality);
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

		private string FFMpegOptionsWith(String codec, uint quality)
		{
			if (quality > 12)
				return "-loglevel quiet -i \"" + Item.FilePath + "\" -acodec " + codec + " -ab " + quality + " \"" + OutputPath + "\"";
			else
				return "-loglevel quiet -i \"" + Item.FilePath + "\" -acodec " + codec + " -aq " + quality + " \"" + OutputPath + "\"";
		}
	}
}

