using System;
using WaveBox.DataModel.Model;

namespace WaveBox.Transcoding
{
	public abstract class FFMpegTranscoder : AbstractTranscoder
	{
		public FFMpegTranscoder(MediaItem item, TranscodeQuality quality) : base(item, quality)
    	{
        	
    	}

		public override string Command 
		{
			get
			{
				return "ffmpeg";
			}
		}

	    protected string FFMpegOptionsWith(String codec, int qualityLevel)
	    {
	        return "-i " + Item.FilePath() + " -acodec " + codec + " -aq " + qualityLevel + " " + OutputPath;
	    }
	}
}

