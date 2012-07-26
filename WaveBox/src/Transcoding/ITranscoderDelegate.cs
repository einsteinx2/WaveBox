using System;

namespace WaveBox.Transcoding
{
	public interface ITranscoderDelegate
	{
		void TranscodeFinished(ITranscoder transcoder);
    	void TranscodeFailed(ITranscoder transcoder);
	}
}

