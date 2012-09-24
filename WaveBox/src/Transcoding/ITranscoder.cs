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
		MP3, 
		AAC, 
		OGG, 

		// Video
		MP4,
		X264,
		MPEGTS
	}

	public static class TranscodeTypeExtensions
	{
		public static bool IsValidForItemType(this TranscodeType transType, ItemType itemType)
		{
			if (itemType == ItemType.Song)
			{
				if (transType == TranscodeType.MP3 ||
					transType == TranscodeType.AAC ||
					transType == TranscodeType.OGG)
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

