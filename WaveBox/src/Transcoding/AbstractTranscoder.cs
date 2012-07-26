using System;
using System.IO;
using System.Threading;
using WaveBox.DataModel.Model;
using System.Diagnostics;

namespace WaveBox.Transcoding
{
	public abstract class AbstractTranscoder : ITranscoder
	{
		private ITranscoderDelegate Delegate { get; set; }

		public MediaItem Item { get; set; }

		public TranscodeState State { get; set; }

		public TranscodeQuality Quality { get; set; }

		public int ReferenceCount { get; set; }

		public abstract TranscodeType Type { get; set; }

		public string OutputPath 
		{
			get 
			{ 
				if (Item != null)
	        	{
	            	string fileName = Item.ItemTypeId + "_" + Item.ItemId + "_" + Type + "_" + Quality;
	            	string path = TranscodeManager.TRANSCODE_PATH + Path.DirectorySeparatorChar + fileName;
	            	//log2File(INFO, "transcoding to " + path);
					return path;
	        	}

	        	return null; 
			} 
		}

		public abstract int? EstimatedBitrate { get; }

		public long? EstimatedOutputSize 
		{ 
			get 
			{
				if (Item != null)
				{
				    return (long)Item.Duration * (long)(EstimatedBitrate / 8);
				}
	        	return null;
			}
		}

		public abstract string Command { get; }

		public abstract string Arguments { get; }

		protected Thread TranscodeThread { get; set; }
		protected Process TranscodeProcess { get; set; }

	    public AbstractTranscoder(MediaItem item, TranscodeQuality quality)
	    {
			State = TranscodeState.None;
	        Item = item;
	        Quality = quality;
	    }

	    public void CancelTranscode()
	    {
	        if (TranscodeProcess != null)
	        {
				// Kill the process
	            TranscodeProcess.Kill();
				TranscodeProcess = null;

				// Wait for the thread to die
				TranscodeThread.Join();
				TranscodeThread = null;
				
				// Set the state
				State = TranscodeState.Canceled;

				// Inform the delegate
			    Delegate.TranscodeFailed(this);
	        }
	    }

	    public void StartTranscode()
		{
			if ((object)TranscodeThread == null || (object)TranscodeProcess == null) 
				return;

			// Start a new thread for the transcode
			TranscodeThread = new Thread(new ThreadStart(Run));
	    }

		public void Run()
	    {
			try 
			{
				// Fork the process
			    TranscodeProcess = new Process();
				TranscodeProcess.StartInfo.FileName = Command;
				TranscodeProcess.StartInfo.Arguments = Arguments;
				TranscodeProcess.Start();

				// Set the state
				State = TranscodeState.Active;

				// Block until done
				TranscodeProcess.WaitForExit();
			}
			catch (Exception e) 
			{
				Console.WriteLine("\t" + "[TRANSCODE] Failed to start thranscode process " + e.InnerException);

				// Set the state
				State = TranscodeState.Failed;

			    // Inform the delegate
			    Delegate.TranscodeFailed(this);
			}

			if (TranscodeProcess != null)
			{
			    int exitValue = TranscodeProcess.ExitCode;
			    if (exitValue == 0)
				{
					State = TranscodeState.Finished;
			        Delegate.TranscodeFinished(this);
				}
			    else
				{
					State = TranscodeState.Failed;
			        Delegate.TranscodeFailed(this);
				}
			}
	    }

		public override bool Equals(Object obj)
	    {
	        // If parameter is null return false.
	        if ((object)obj == null)
	        {
	            return false;
	        }

	        // If parameter cannot be cast to DelayedOperation return false.
	        AbstractTranscoder op = obj as AbstractTranscoder;
	        if ((object)op == null)
	        {
	            return false;
	        }

	        // Return true if the fields match:
	        return Equals(op);
	    }

	    public bool Equals(AbstractTranscoder op)
	    {
	        // If parameter is null return false:
	        if ((object)op == null)
	        {
	            return false;
	        }

	        // Return true if they match
			return Item.Equals(op.Item) && Type == op.Type && Quality == op.Quality;
	    }

	    public override int GetHashCode()
	    {
			return OutputPath.GetHashCode();
	    }
	}
}

