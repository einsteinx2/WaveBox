using System;
using WaveBox.DataModel.Model;
using NLog;

namespace WaveBox.Transcoding
{
	public class FFMpegMpegtsTranscoder : VideoTranscoder
	{		
		//private static Logger logger = LogManager.GetCurrentClassLogger();

		public override TranscodeType Type { get { return TranscodeType.MPEGTS; } }

		public override string Codec { get { return "libx264"; } }

		public override string Command { get { return "ffmpeg"; } }
		
		public override string OutputExtension { get { return "ts"; } }

		public override string MimeType { get { return "video/MP2T"; } }

		public FFMpegMpegtsTranscoder(IMediaItem item, uint quality, bool isDirect, uint? width, uint? height, bool maintainAspect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, width, height, maintainAspect, offsetSeconds, lengthSeconds)
		{
		}

		protected override string GenerateArguments(uint abitrate, uint vbitrate, uint width, uint height)
		{
			// Note: really weird... it seems like the order of the ffmpeg options greatly impacts it's performance,
			// as the first line and last line are the same, but the first line works well, last line keeps a bunch
			// of zombie ffmpeg processes, runs at 100% cpu, and works poorly...
			//
			//return " -ss " + OffsetSeconds + " -t " + LengthSeconds + " -i \"" + Item.FilePath + "\" -async 1 -b " + vbitrate + " -s " + width + "x" + height + " -ar 44100 -ac 2 -v 0 -f mpegts -vcodec libx264 -preset superfast -acodec libmp3lame -threads 0 " + OutputPath;
			//return " -ss " + OffsetSeconds + " -t " + LengthSeconds + " -i \"" + Item.FilePath + "\" -async 1 -b " + vbitrate + " -s " + width + "x" + height + " -ar 44100 -ab " + abitrate + " -ac 2 -v 0 -f mpegts -vcodec libx264 -preset superfast -acodec libmp3lame -threads 0 " + OutputPath;

			return " -ss " + OffsetSeconds + " -t " + LengthSeconds + " -i \"" + Item.FilePath + "\" -async 1 -b " + vbitrate + " -s " + width + "x" + height + " -ar 44100 -ab " + abitrate + " -ac 2 -v 0 -f mpegts -vcodec " + Codec + " -preset superfast -strict experimental -acodec aac -threads 0 " + OutputPath;

			//return "-i \"" + Item.FilePath + "\" -async 1 -b " + vbitrate + " -s " + width + "x" + height + " -ss " + OffsetSeconds + " -t " + LengthSeconds + " -acodec libmp3lame -ar 44100 -ac 2 -ab " + abitrate + " -f mpegts -vcodec libx264 -preset superfast -threads 0 " + OutputPath;
			//return "-i \"" + Item.FilePath + "\" -async 1 -b " + vbitrate + " -s " + width + "x" + height + " -ss " + OffsetSeconds + " -t " + LengthSeconds + " -acodec libmp3lame -ar 44100 -ac 2 -aq " + "5" + " -f mpegts -vcodec libx264 -preset superfast -threads 0 " + OutputPath;
			//return "-i \"" + Item.FilePath + "\" -async 1 -b " + vbitrate + " -s " + width + "x" + height + " -ss " + OffsetSeconds + " -t " + LengthSeconds + " -acodec libmp3lame -ar 44100 -ac 2 -f mpegts -vcodec libx264 -preset superfast -threads 0 " + OutputPath;
		}

		// Estimate high
		public override uint? EstimatedBitrate
		{
			get
			{
				if ((object)Quality == null)
					return null;
				
				return (uint)((double)TotalBitrateForQuality(Quality) * 1.5);
			}
		}
	}
}

