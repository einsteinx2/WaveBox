using System;
using WaveBox.DataModel.Model;

namespace WaveBox.Transcoding
{
	public class FFMpegX264Transcoder : AbstractTranscoder
	{
		public override TranscodeType Type { get { return TranscodeType.X264; } }

		public override string Command { get { return "ffmpeg"; } }

		public uint? Width { get; set; } 

		public uint? Height { get; set; } 

		public bool MaintainAspect { get; set; }
		
		public FFMpegX264Transcoder(IMediaItem item, uint quality, uint? width, uint? height, bool maintainAspect) : base(item, quality)
		{
			Width = width;
			Height = height;
			MaintainAspect = maintainAspect;
		}
		
		public override string Arguments
		{
			get 
			{ 
				uint totalBitrate = TotalBitrateForQuality(Quality);

				// Start with the requested width and height
				Video video = (Video)Item;
				uint? width = Width;
				uint? height = Height;

				if ((object)width == null && (object)height == null)
				{
					// If neither width nor height are specified, use the original values from the video
					width = (uint)video.Width;
					height = (uint)video.Height;
				}
				else if ((object)width == null)
				{
					// If no width is specified, pick the height based on the width
					width = MaintainAspect ? (uint)((float)height * video.AspectRatio) : (uint)video.Width;
				}
				else if ((object)height == null)
				{
					// If no height is specified, pick the width based on the height
					height = MaintainAspect ? (uint)((float)width / video.AspectRatio) : (uint)video.Height;
				}
				else if (MaintainAspect)
				{
					// If both are specified, and the aspect ratio should be maintained, make sure it is correct, based on width
					if (MaintainAspect)
						width = (uint)((float)height * video.AspectRatio);
				}

				// The user entered an actual bitrate number, so calculate the audio and video bitrates
				uint abitrate = CalculateAudioBitrate(totalBitrate);
				uint vbitrate = Quality - abitrate;
				return GenerateArguments(abitrate * 1024, vbitrate * 1024, (uint)width, (uint)height);
			}
		}

		private uint TotalBitrateForQuality(uint quality)
		{
			switch (Quality)
			{
				case (uint)TranscodeQuality.Low:     return 256;
				case (uint)TranscodeQuality.Medium:  return 512;
				case (uint)TranscodeQuality.High:    return 1024;
				case (uint)TranscodeQuality.Extreme: return 2048;
				default: return quality;
			}
		}

		// We're going to want to eventually do some automatic video size choosing based on the bitrate
		// when the width and height are not explicitly specified
		/*private uint ChooseWidth(uint? width, uint )
		{
			// Choose a width for the transcode

		}

		private uint ChooseHeight(uint? width)
		{
			// Choose a height for the transcode based on the chosen width

		}*/

		private uint CalculateAudioBitrate(uint totalBitrate)
		{
			// Calculate the portion of the audio bitrate to use for the given total bitrate
			return (uint)(2.6899 * Math.Pow((double)totalBitrate, -0.44) * totalBitrate);
		}
		
		public override uint? EstimatedBitrate
		{
			get
			{
				if ((object)Quality == null)
					return null;

				return TotalBitrateForQuality(Quality);
			}
		}
		
		public override string OutputExtension
		{
			get
			{
				return "mp4";
			}
		}

		private string GenerateArguments(uint abitrate, uint vbitrate, uint width, uint height)
		{
			//return "-v 0 -i \"" + Item.FilePath + "\" -async 1 -ar 44100 -ab " + abitrate + " -vcodec libx264 -b " + vbitrate + " -s " + width + "x" + height + " -f flv -preset superfast -threads 0 \"" + OutputPath + "\"";

			//return "-i \"" + Item.FilePath + "\" -vcodec libx264 -crf 21 -acodec aac -ac 2 -ab 192000 -strict experimental -refs 3 -threads 4 \"" + OutputPath + "\"";

			return "-i \"" + Item.FilePath + "\" -async 1 -b " + vbitrate + " -s " + width + "x" + height + " -ar 44100 -ac 2 -v 0 -f mpegts -refs 3 -vcodec libx264 -preset superfast -threads 0 \"" + OutputPath + "\"";

			// multi-threaded
			//return "-threads 8 -loglevel quiet -i \"" + Item.FilePath + "\" -ab " + abitrate + " -vcodec libx264 -b " + vbitrate + " -s " + width + "x" + height + " \"" + OutputPath + "\"";
		}
	}
}

