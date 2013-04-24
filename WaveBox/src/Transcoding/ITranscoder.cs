using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using WaveBox.DataModel.Model;

namespace WaveBox.Transcoding
{
	public enum TranscodeState
	{
		Active,
		Canceled,
		Finished,
		Failed,
		None
	}

	public enum TranscodeQuality
	{
		Low = 0, 
		Medium = 1, 
		High = 2, 
		Extreme = 3
	}

	public enum TranscodeType
	{
		// Audio
		AAC,
		MP3,
		OGG,
		OPUS,

		// Video
		MP4,
		MPEGTS,
		X264,

		// Bad type (for defaults)
		UNKNOWN
	}

	public static class TranscodeTypeExtensions
	{
		public static bool IsValidForItemType(this TranscodeType transType, ItemType itemType)
		{
			if (itemType == ItemType.Song)
			{
				if (transType == TranscodeType.MP3 ||
					transType == TranscodeType.AAC ||
					transType == TranscodeType.OGG ||
					transType == TranscodeType.OPUS)
				{
					return true;
				}
			}
			else if (itemType == ItemType.Video)
			{
				if (transType == TranscodeType.MP4 ||
					transType == TranscodeType.X264 ||
					transType == TranscodeType.MPEGTS)
				{
					return true;
				}
			}

			return false;
		}

		// Return transcode type parsed from input string
		public static TranscodeType StringToTranscodeType(string transType)
		{
			// Try to parse into a TranscodeType
			TranscodeType result;
			if (!Enum.TryParse(transType, out result))
			{
				return TranscodeType.UNKNOWN;
			}

			return result;
		}
	}

	public interface ITranscoder
	{
		TranscodeState State { get; set; }
		IMediaItem Item { get; set; }
		TranscodeType Type { get; }
		bool IsDirect { get; set; }
		uint Quality { get; set; }
		string MimeType { get; }
		string OutputExtension { get; }
		string OutputFilename { get; }
		string OutputPath { get; }
		uint? EstimatedBitrate { get; }
		long? EstimatedOutputSize { get; }
		int ReferenceCount { get; set; }
		Thread TranscodeThread { get; set; }
		Process TranscodeProcess { get; set; }

		void StartTranscode();
		void CancelTranscode();
	}
}

