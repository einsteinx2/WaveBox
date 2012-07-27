using System;
using WaveBox.DataModel.Model;

namespace WaveBox.Transcoding
{
	public class FFMpegMP3Transcoder : FFMpegTranscoder
	{
		public override TranscodeType Type
		{
			get { return TranscodeType.MP3; }
			set { }
		}

		public FFMpegMP3Transcoder(MediaItem item, TranscodeQuality quality) : base(item, quality)
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
		            case TranscodeQuality.Low:
		                // VBR - V9 quality (~64 kbps)
		                options = FFMpegOptionsWith(codec, 9);
						break;
		            case TranscodeQuality.Medium:
		                // VBR - V5 quality (~128 kbps)
		                options = FFMpegOptionsWith(codec, 5);
						break;
		            case TranscodeQuality.High:
		                // VBR - V2 quality (~192 kbps)
		                options = FFMpegOptionsWith(codec, 2);
						break;
		            case TranscodeQuality.Extreme:
		                // VBR - V0 quality (~224 kbps)
		                options = FFMpegOptionsWith(codec, 0);
						break;
		        }
		        return options;
			}
	    }

	    public override int? EstimatedBitrate
	    {
			get 
			{ 
				int? bitrate = null;
		        switch (Quality)
		        {
		            case TranscodeQuality.Low: 
						bitrate = 64;
						break;
		            case TranscodeQuality.Medium: 
						bitrate = 128;		
						break;
		            case TranscodeQuality.High: 
						bitrate = 192;
						break;
		            case TranscodeQuality.Extreme: 
						bitrate = 224;
						break;
		        }
		        return bitrate;
			}
	    }
	}
}

