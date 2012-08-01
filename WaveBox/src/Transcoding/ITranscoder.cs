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
		HLS
	}

	public enum TranscodeQuality
	{
		Low, 
		Medium, 
		High, 
		Extreme
	}

	public interface ITranscoder
	{
		TranscodeState State { get; set; }
		MediaItem Item { get; set; }
		TranscodeType Type { get; set; }
		TranscodeQuality Quality { get; }
		string OutputExtension { get; }
		string OutputFilename { get; }
		string OutputPath { get; }
		int? EstimatedBitrate { get; }
		long? EstimatedOutputSize { get; }
		int ReferenceCount { get; set; }

		void StartTranscode();
		void CancelTranscode();
	}
}

