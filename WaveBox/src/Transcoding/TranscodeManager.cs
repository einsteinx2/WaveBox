using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using WaveBox.DataModel.Model;
using System.Threading;
using NLog;

namespace WaveBox.Transcoding
{
	public class TranscodeManager
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

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
		
		public ITranscoder TranscodeSong(IMediaItem song, TranscodeType type, uint quality, bool isDirect, uint offsetSeconds, uint lengthSeconds)
		{
			logger.Info("[TRANSCODE] Asked to transcode song: " + song.FileName);
			lock (transcoders) 
			{
				ITranscoder transcoder = null;
				switch (type)
				{
					case TranscodeType.MP3:
						transcoder = new FFMpegMP3Transcoder(song, quality, isDirect, offsetSeconds, lengthSeconds);
						break;
					case TranscodeType.OGG:
						transcoder = new FFMpegOGGTranscoder(song, quality, isDirect, offsetSeconds, lengthSeconds);
						break;
					case TranscodeType.OPUS:
						transcoder = new FFMpegOpusTranscoder(song, quality, isDirect, offsetSeconds, lengthSeconds);
						break;
					case TranscodeType.AAC: 
						transcoder = new FFMpegAACTranscoder(song, quality, isDirect, offsetSeconds, lengthSeconds);
						break;
				}

				transcoder = StartTranscoder(transcoder);

				return transcoder;
			}
		}

		public ITranscoder TranscodeVideo(IMediaItem video, TranscodeType type, uint quality, bool isDirect, uint? width, uint? height, bool maintainAspect, uint offsetSeconds, uint lengthSeconds)
		{
			logger.Info("[TRANSCODE] Asked to transcode video: " + video.FileName);
			lock (transcoders) 
			{
				ITranscoder transcoder = null;;
				switch (type)
				{
					case TranscodeType.X264: 
						transcoder = new FFMpegX264Transcoder(video, quality, isDirect, width, height, maintainAspect, offsetSeconds, lengthSeconds);
						break;
					case TranscodeType.MPEGTS: 
						transcoder = new FFMpegMpegtsTranscoder(video, quality, isDirect, width, height, maintainAspect, offsetSeconds, lengthSeconds);
						break;
				}
				
				transcoder = StartTranscoder(transcoder);
				
				return transcoder;
			}
		}

		private ITranscoder StartTranscoder(ITranscoder inTranscoder)
		{
			ITranscoder transcoder = inTranscoder;
			if ((object)transcoder != null)
			{
				// Don't reuse direct transcoders
				if (!transcoder.IsDirect && transcoders.Contains(transcoder))
				{
					logger.Info("[TRANSCODE] Using existing transcoder");
					
					// Get the existing transcoder
					int index = transcoders.IndexOf(transcoder);
					transcoder = transcoders[index];
					
					// Increment the reference count
					transcoder.ReferenceCount++;
				}
				else
				{
					logger.Info("[TRANSCODE] Creating a new transcoder");

					// Add the transcoder to the array
					transcoders.Add(transcoder);
					
					// Increment the reference count
					transcoder.ReferenceCount++;

					// Start the transcode process
					transcoder.StartTranscode();
				}
			}
			return transcoder;
		}

		public void ConsumedTranscode(ITranscoder transcoder)
		{
			logger.Info("Waiting on {0} for 30 more seconds... State: {1}", transcoder.Item.FileName, transcoder.State);

			for (int i = 30; i > 0; i--)
			{
				Thread.Sleep(1000);
			}
			// Do nothing if the transcoder is null or is a stdout transcoder
			if ((object)transcoder == null)
			{
				return;
			}

			if (transcoder.IsDirect && transcoder.State == TranscodeState.Active)
			{
				try
				{
					// Kill the running transcode
					transcoder.TranscodeProcess.Kill();
				}
				catch{}
			}

			lock (transcoders)
			{
				logger.Info("[TRANSCODE] Consumed transcoder for " + transcoder.Item.FileName);
				
				// Decrement the reference count
				transcoder.ReferenceCount--;
				
				if (transcoder.ReferenceCount == 0) 
				{
					// No other clients need this file, remove it
					transcoders.Remove(transcoder);

					if (!transcoder.IsDirect)
					{
						// Remove the file
						File.Delete(transcoder.OutputPath);
					}
				}
			}
		}

		public void CancelTranscode(ITranscoder transcoder)
		{
			// Do nothing if the transcoder is null or is a stdout transcoder
			if ((object)transcoder == null)
				return;

			lock (transcoders)
			{
				logger.Info("[TRANSCODE] Cancelling transcoder for " + transcoder.Item.FileName);

				if (transcoder.ReferenceCount == 1)
				{
					// No one else is using this transcoder, so cancel it
					transcoder.CancelTranscode();
				}

				// Consume the transcoder
				ConsumedTranscode(transcoder);
			}
		}

		public void CancelAllTranscodes()
		{
			List<ITranscoder> tempTranscoders = new List<ITranscoder>();
			tempTranscoders.AddRange(transcoders);
			foreach (ITranscoder transcoder in tempTranscoders)
			{
				CancelTranscode(transcoder);
			}
		}

		/*
		 * Transcoder delegate
		 */

		public void TranscodeFinished(ITranscoder transcoder)
		{
			// Do something
			logger.Info("[TRANSCODE] Transcode finished for " + transcoder.Item.FileName);
		}

		public void TranscodeFailed(ITranscoder transcoder)
		{
			// Do something
			logger.Info("[TRANSCODE] Transcode failed for " + transcoder.Item.FileName);
		}
	}
}

