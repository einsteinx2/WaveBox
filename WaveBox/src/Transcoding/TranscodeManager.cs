using System;
using System.IO;
using System.Collections.Generic;
using WaveBox.DataModel.Model;

namespace WaveBox.Transcoding
{
	public class TranscodeManager
	{
	    public const string TRANSCODE_PATH = "trans";

		private TranscodeManager() { }
	    private static readonly TranscodeManager instance = new TranscodeManager();
		public static TranscodeManager Instance { get { return Instance; } }

	    private IList<ITranscoder> transcoders = new List<ITranscoder>();

		public ITranscoder CreateTranscoder(MediaItem item, TranscodeType type, TranscodeQuality quality)
	    {
	        switch (type)
	        {
	            case TranscodeType.MP3: 
					return new FFMpegMP3Transcoder(item, quality);
	            //case TranscodeType.AAC: 
				//	return new FFMpegAACTranscoder(item, quality);
	            //case TranscodeType.OGG: 
				//	return new FFMpegOGGTranscoder(item, quality);
	            //case TranscodeType.HLS: 
				//	return new FFMpegHLSTranscoder(item, quality);
	        }
	        return null;
	    }

	    public ITranscoder TranscodeItem(MediaItem item, TranscodeType type, TranscodeQuality quality)
		{
			lock (transcoders) 
			{
				ITranscoder transcoder = CreateTranscoder(item, type, quality);

				if (transcoders.Contains(transcoder))
				{
					// Get the existing transcoder
					int index = transcoders.IndexOf(transcoder);
					transcoder = transcoders[index];

					// Increment the reference count
					transcoder.ReferenceCount++;
				}
				else
				{
					// Increment the reference count
					transcoder.ReferenceCount++;

					// Start the transcode process
		            transcoder.StartTranscode();
				}

		        return transcoder;
			}
	    }

	    public void ConsumedTranscode(ITranscoder transcoder)
		{
			lock (transcoders)
			{
				// Decrement the reference count
				transcoder.ReferenceCount--;

				if (transcoder.ReferenceCount == 0) 
				{
					// No other clients need this file, remove it
					transcoders.Remove(transcoder);

					// Remove the file
					File.Delete(transcoder.OutputPath);
				}
			}
	    }

	    public void CancelTranscode(ITranscoder transcoder)
		{
			lock (transcoders)
			{
				if (transcoder.ReferenceCount == 1)
				{
					// No one else is using this transcoder, so cancel it
					transcoder.CancelTranscode();
				}

				// Consume the transcoder
				ConsumedTranscode(transcoder);
			}
	    }

	    /*
	     * Transcoder delegate
	     */

	    public void TranscodeFinished(ITranscoder transcoder)
	    {
			// Do something
	    }

	    public void TranscodeFailed(ITranscoder transcoder)
	    {
	        // Do something
	    }
	}
}

