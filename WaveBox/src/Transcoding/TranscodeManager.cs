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
		public static TranscodeManager Instance { get { return instance; } }

	    private IList<ITranscoder> transcoders = new List<ITranscoder>();

		public void Setup()
		{
			if (!Directory.Exists(TRANSCODE_PATH)) 
			{
				Directory.CreateDirectory(TRANSCODE_PATH);
			}
		}

		public ITranscoder CreateSongTranscoder(IMediaItem song, TranscodeType type, uint quality)
	    {
			Console.WriteLine("[TRANSCODE] Creating transcoder for song: " + song.FileName);
	        switch (type)
	        {
	            case TranscodeType.MP3: 
					return new FFMpegMP3Transcoder(song, quality);
	            //case TranscodeType.AAC: 
				//	return new FFMpegAACTranscoder(item, quality);
	            //case TranscodeType.OGG: 
				//	return new FFMpegOGGTranscoder(item, quality);
				//case TranscodeType.MP4:
				//	return new 
	            //case TranscodeType.HLS: 
				//	return new FFMpegHLSTranscoder(item, quality);
	        }
	        return null;
	    }

		public ITranscoder CreateVideoTranscoder(IMediaItem video, TranscodeType type, uint quality, uint? width, uint? height, bool maintainAspect)
		{
			Console.WriteLine("[TRANSCODE] Creating transcoder for video: " + video.FileName);
			switch (type)
			{
				case TranscodeType.X264: 
					return new FFMpegX264Transcoder(video, quality, width, height, maintainAspect);
				//case TranscodeType.HLS: 
				//	return new FFMpegHLSTranscoder(item, quality);
			}
			return null;
		}

		public ITranscoder TranscodeSong(IMediaItem song, TranscodeType type, uint quality)
		{
			Console.WriteLine("[TRANSCODE] Asked to transcode song: " + song.FileName);
			lock (transcoders) 
			{
				ITranscoder transcoder = CreateSongTranscoder(song, type, quality);

				StartTranscoder(transcoder);

		        return transcoder;
			}
	    }

		public ITranscoder TranscodeVideo(IMediaItem video, TranscodeType type, uint quality, uint? width, uint? height, bool maintainAspect)
		{
			Console.WriteLine("[TRANSCODE] Asked to transcode video: " + video.FileName);
			lock (transcoders) 
			{
				ITranscoder transcoder = CreateVideoTranscoder(video, type, quality, width, height, maintainAspect);
				
				StartTranscoder(transcoder);
				
				return transcoder;
			}
		}

		private void StartTranscoder(ITranscoder transcoder)
		{
			if ((object)transcoder != null)
			{
				if (transcoders.Contains(transcoder))
				{
					Console.WriteLine("[TRANSCODE] Using existing transcoder");
					
					// Get the existing transcoder
					int index = transcoders.IndexOf(transcoder);
					transcoder = transcoders[index];
					
					// Increment the reference count
					transcoder.ReferenceCount++;
				}
				else
				{
					Console.WriteLine("[TRANSCODE] Creating a new transcoder");

					// Add the transcoder to the array
					transcoders.Add(transcoder);
					
					// Increment the reference count
					transcoder.ReferenceCount++;
					
					// Start the transcode process
					transcoder.StartTranscode();
				}
			}
		}

	    public void ConsumedTranscode(ITranscoder transcoder)
		{
			if ((object)transcoder == null)
				return;

			lock (transcoders)
			{
				Console.WriteLine("[TRANSCODE] Consumed transcoder for " + transcoder.Item.FileName);

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
			if ((object)transcoder == null)
				return;

			lock (transcoders)
			{
				Console.WriteLine("[TRANSCODE] Cancelling transcoder for " + transcoder.Item.FileName);

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
			Console.WriteLine("[TRANSCODE] Transcode finished for " + transcoder.Item.FileName);
	    }

	    public void TranscodeFailed(ITranscoder transcoder)
	    {
	        // Do something
			Console.WriteLine("[TRANSCODE] Transcode failed for " + transcoder.Item.FileName);
	    }
	}
}

