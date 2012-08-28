using System;
using WaveBox.DataModel.Model;

namespace WaveBox.Transcoding
{
	public abstract class FFMpegTranscoder : AbstractTranscoder
	{
		public override string Command { get { return "ffmpeg"; } }

		public FFMpegTranscoder(MediaItem item, TranscodeQuality quality) : base(item, quality)
    	{
        	
    	}

	    protected string FFMpegOptionsWith(String codec, int qualityLevel)
	    {
	        return "-i \"" + Item.FilePath + "\" -acodec " + codec + " -aq " + qualityLevel + " \"" + OutputPath + "\"";
	    }
	}
}

