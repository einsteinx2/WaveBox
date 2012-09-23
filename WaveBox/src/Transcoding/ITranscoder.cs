using System;
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

	public enum TranscodeType
	{
		MP3, 
		AAC, 
		OGG, 
		MP4,
		X264,
		HLS
	}

	public enum TranscodeQuality
	{
		Low = 0, 
		Medium = 1, 
		High = 2, 
		Extreme = 3
	}

	public interface ITranscoder
	{
		TranscodeState State { get; set; }
		IMediaItem Item { get; set; }
		TranscodeType Type { get; }
		uint Quality { get; set; }
		string OutputExtension { get; }
		string OutputFilename { get; }
		string OutputPath { get; }
		uint? EstimatedBitrate { get; }
		long? EstimatedOutputSize { get; }
		int ReferenceCount { get; set; }

		void StartTranscode();
		void CancelTranscode();
	}
}

