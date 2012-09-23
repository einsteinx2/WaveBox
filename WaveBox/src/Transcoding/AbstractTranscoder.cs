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

		public IMediaItem Item { get; set; }

		public TranscodeState State { get; set; }

		// This is either a TranscodeQuality enum value or if higher, a constant bitrate
		public uint Quality { get; set; }

		public int ReferenceCount { get; set; }

		public abstract TranscodeType Type { get; }

		public abstract string OutputExtension { get; }

		public string OutputFilename
		{
			get
			{
				return Item.ItemTypeId + "_" + Item.ItemId + "_" + Type + "_" + Quality + "." + OutputExtension;

			}
		}

		public string OutputPath 
		{
			get 
			{ 
				if (Item != null)
	        	{
	            	string path = TranscodeManager.TRANSCODE_PATH + Path.DirectorySeparatorChar + OutputFilename;
	            	//log2File(INFO, "transcoding to " + path);
					return path;
	        	}

	        	return null; 
			} 
		}

		public abstract uint? EstimatedBitrate { get; }

		public long? EstimatedOutputSize 
		{ 
			get 
			{
				if (Item != null)
				{
					Console.WriteLine("Item.Duration: " + Item.Duration + "  EstimatedBitrate: " + EstimatedBitrate);
				    return (long)Item.Duration * (long)(EstimatedBitrate * 128);
				}
	        	return null;
			}
		}

		public abstract string Command { get; }

		public abstract string Arguments { get; }

		protected Thread TranscodeThread { get; set; }
		protected Process TranscodeProcess { get; set; }

		public AbstractTranscoder(IMediaItem item, uint quality)
	    {
			State = TranscodeState.None;
	        Item = item;
	        Quality = quality;
	    }

	    public void CancelTranscode()
	    {
	        if (TranscodeProcess != null)
	        {
				Console.WriteLine("[TRANSCODE] cancelling transcode for " + Item.FileName);

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
			if ((object)TranscodeThread != null || (object)TranscodeProcess != null) 
				return;

			Console.WriteLine("[TRANSCODE] starting transcode for " + Item.FileName);

			// Set the state
			State = TranscodeState.Active;

			// Delete any existing file of this name
			File.Delete(OutputPath);

			// Start a new thread for the transcode
			TranscodeThread = new Thread(new ThreadStart(Run));
			TranscodeThread.Start();
	    }

		public void Run()
	    {
			try 
			{
				Console.WriteLine("[TRANSCODE] Forking the process");
				Console.WriteLine("[TRANSCODE] " + Command + " " + Arguments);

				// Fork the process
			    TranscodeProcess = new Process();
				TranscodeProcess.StartInfo.FileName = Command;
				TranscodeProcess.StartInfo.Arguments = Arguments;
				TranscodeProcess.Start();

				Console.WriteLine("[TRANSCODE] Waiting for process to finish");

				// Block until done
				TranscodeProcess.WaitForExit();

				Console.WriteLine("[TRANSCODE] Process finished");
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
			    Console.WriteLine("[TRANSCODE] exit value " + exitValue);

				if (exitValue == 0)
				{
					State = TranscodeState.Finished;

					if ((object)Delegate != null)
			        	Delegate.TranscodeFinished(this);
				}
			    else
				{
					State = TranscodeState.Failed;
			        
					if ((object)Delegate != null)
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

