using System;
using WaveBox.DataModel.Model;

namespace WaveBox.Transcoding
{
	public abstract class VideoTranscoder : AbstractTranscoder
	{
		//private static Logger logger = LogManager.GetCurrentClassLogger();

		public uint? Width { get; set; } 
		
		public uint? Height { get; set; } 
		
		public bool MaintainAspect { get; set; }

		public VideoTranscoder(IMediaItem item, uint quality, bool isDirect, uint? width, uint? height, bool maintainAspect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, offsetSeconds, lengthSeconds)
		{
			Width = width;
			Height = height;
			MaintainAspect = maintainAspect;
			OffsetSeconds = offsetSeconds;
			LengthSeconds = lengthSeconds;
		}

		public override string Arguments
		{
			get 
			{ 
				// Make sure this is actually a video
				Video video = Item as Video;
				if ((object)video == null)
				{
					return null;
				}

				// Start with the requested width and height
				uint? width = Width;
				uint? height = Height;
				uint totalBitrate = TotalBitrateForQuality(Quality);
				
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
					{
						width = (uint)((float)height * video.AspectRatio);
					}
				}
				
				// The user entered an actual bitrate number, so calculate the audio and video bitrates
				uint abitrate = CalculateAudioBitrate(totalBitrate);
				uint vbitrate = totalBitrate - abitrate;
				return GenerateArguments(abitrate * 1024, vbitrate * 1024, (uint)width, (uint)height);
			}
		}

		public static uint DefaultBitrateForQuality(uint quality)
		{
			switch (quality)
			{
				case (uint)TranscodeQuality.Low:     return 256;
				case (uint)TranscodeQuality.Medium:  return 512;
				case (uint)TranscodeQuality.High:    return 1024;
				case (uint)TranscodeQuality.Extreme: return 2048;
				default: return quality;
			}
		}

		// Pick some good default bitrates for the quality levels, but let individual transcoder types 
		// override this if necessary
		public virtual uint TotalBitrateForQuality(uint quality)
		{
			return VideoTranscoder.DefaultBitrateForQuality(quality);
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
		
		protected uint CalculateAudioBitrate(uint totalBitrate)
		{
			// Calculate the portion of the audio bitrate to use for the given total bitrate
			return (uint)(2.6899 * Math.Pow((double)totalBitrate, -0.44) * totalBitrate);
		}
		
		public override uint? EstimatedBitrate
		{
			get
			{
				if ((object)Quality == null)
				{
					return null;
				}
				
				return TotalBitrateForQuality(Quality);
			}
		}

		protected abstract string GenerateArguments(uint abitrate, uint vbitrate, uint width, uint height);

		public override bool Equals(Object obj)
		{
			// If they are the exact same object, return true
			if (Object.ReferenceEquals(this, obj))
			{
				return true;
			}
			else if (IsDirect)
			{
				// If this is a direct transcoder, only use reference equality
				return false;
			}

			// If parameter is null return false.
			if ((object)obj == null)
			{
				return false;
			}

			// If the types don't match exactly, return false
			if (this.GetType() != obj.GetType())
			{
				return false;
			}
			
			// If parameter cannot be cast to AbstractTranscoder return false.
			VideoTranscoder op = obj as VideoTranscoder;
			if ((object)op == null)
			{
				return false;
			}
			
			// Return true if the fields match:
			return Equals(op);
		}
		
		public bool Equals(VideoTranscoder op)
		{
			// If parameter is null return false:
			if ((object)op == null)
			{
				return false;
			}
			
			// Return true if they match
			return Item.Equals(op.Item) && Type == op.Type && Quality == op.Quality && Width == op.Width && Height == op.Height && MaintainAspect == op.MaintainAspect;
		}
		
		public override int GetHashCode()
		{
			int hash = 13;
			hash = (hash * 7) + (Item == null ? Item.GetHashCode() : 0);
			hash = (hash * 7) + Type.GetHashCode();
			hash = (hash * 7) + Quality.GetHashCode();
			hash = (hash * 7) + Width.GetHashCode();
			hash = (hash * 7) + Height.GetHashCode();
			hash = (hash * 7) + MaintainAspect.GetHashCode();
			return hash;
		}
	}
}

