using System;
using WaveBox.DataModel.Model;
using System.Diagnostics;
using System.IO;
using NLog;

namespace WaveBox.Transcoding
{
	public class FFMpegNeroAACTranscoder : AbstractTranscoder
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public override TranscodeType Type { get { return TranscodeType.AAC; } }

		// Probably will never be needed, but placeholder in case we use ffmpeg AAC encoders
		public override string Codec { get { return "neroaac"; } }

		public override string Command { get { return "ffmpeg"; } }
		
		public override string OutputExtension { get { return "mp4"; } }
		
		public override string MimeType { get { return "audio/mp4"; } }

		private Process FfmpegProcess;
		
		public FFMpegNeroAACTranscoder(IMediaItem item, uint quality, bool isDirect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, offsetSeconds, lengthSeconds)
		{
			
		}

		public override void Run()
		{
			try 
			{
				string ffmpegArguments = "-loglevel quiet -i \"" + Item.FilePath + "\" -f wav -";
				logger.Info("[TRANSCODE] Forking the process");
				logger.Info("[TRANSCODE] " + "ffmpeg " + ffmpegArguments);
				
				// Create the ffmpeg process
				FfmpegProcess = new Process();
				FfmpegProcess.StartInfo.FileName = "ffmpeg";
				FfmpegProcess.StartInfo.Arguments = ffmpegArguments;
				FfmpegProcess.StartInfo.UseShellExecute = false;
				FfmpegProcess.StartInfo.RedirectStandardOutput = true;
				FfmpegProcess.StartInfo.RedirectStandardError = true;

				// Create the neroAacEnc object
				logger.Info("neroAacEnc " + Arguments);
				TranscodeProcess = new Process();
				TranscodeProcess.StartInfo.FileName = "neroAacEnc";
				TranscodeProcess.StartInfo.Arguments = Arguments;
				TranscodeProcess.StartInfo.UseShellExecute = false;
				TranscodeProcess.StartInfo.RedirectStandardInput = true;
				TranscodeProcess.StartInfo.RedirectStandardOutput = false;
				TranscodeProcess.StartInfo.RedirectStandardError = false;

				var buffer = new byte[8192];
				FfmpegProcess.Start();
				TranscodeProcess.Start();
				
				var input = new BinaryWriter(TranscodeProcess.StandardInput.BaseStream);
				int totalWritten = 0;

				while (true)
				{
					int bytesRead = FfmpegProcess.StandardOutput.BaseStream.Read(buffer, 0, 8192);
					totalWritten += bytesRead;
					if (bytesRead > 0)
					{
						input.Write(buffer, 0, bytesRead);
					}
					//logger.Info("{0} bytes written to buffer ({1} this iteration)", totalWritten, bytesRead);

					if (bytesRead == 0 && FfmpegProcess.HasExited)
					{
						input.Close();
						FfmpegProcess.Close();
						break;
					}
				}
				
				logger.Info("[TRANSCODE] Waiting for processes to finish");
				
				// Block until done
				TranscodeProcess.WaitForExit();
				
				logger.Info("[TRANSCODE] Process finished");
			}
			catch (Exception e) 
			{
				logger.Info("\t" + "[TRANSCODE] Failed to start transcode process " + e);

				try
				{
					TranscodeProcess.Kill();
					TranscodeProcess.Close();
				}
				catch { /* do nothing if killing the process fails */ }

				try
				{
					FfmpegProcess.Kill();
					FfmpegProcess.Close();
				}
				catch { /* do nothing if killing the process fails */ }

				// Set the state
				State = TranscodeState.Failed;
				
				// Inform the delegate
				if ((object)TranscoderDelegate != null)
				{
					TranscoderDelegate.TranscodeFailed(this);
				}
				
				return;
			}
			
			if (TranscodeProcess != null)
			{
				int exitValue = TranscodeProcess.ExitCode;
				logger.Info("[TRANSCODE] exit value " + exitValue);
				
				if (exitValue == 0)
				{
					State = TranscodeState.Finished;
					
					if ((object)TranscoderDelegate != null)
					{
						TranscoderDelegate.TranscodeFinished(this);
					}
				}
				else
				{
					State = TranscodeState.Failed;
					
					if ((object)TranscoderDelegate != null)
					{
						TranscoderDelegate.TranscodeFailed(this);
					}
				}
			}
		}
		
		public override string Arguments
		{
			get 
			{ 
				string options = null;
				switch (Quality)
				{
					case (uint)TranscodeQuality.Low:
						// VBR - V9 quality (~64 kbps)
						options = NeroAacEncOptions(64 * 1000);
						break;
					case (uint)TranscodeQuality.Medium:
						// VBR - V5 quality (~128 kbps)
						options = NeroAacEncOptions(96 * 1000);
						break;
					case (uint)TranscodeQuality.High:
						// VBR - V2 quality (~192 kbps)
						options = NeroAacEncOptions(128 * 1000);
						break;
					case (uint)TranscodeQuality.Extreme:
						// VBR - V0 quality (~224 kbps)
						options = NeroAacEncOptions(160 * 1000);
						break;
					default:
						options = NeroAacEncOptions(Quality * 1000);
						break;
				}
				return options;
			}
		}
		
		public override uint? EstimatedBitrate
		{
			get 
			{ 
				uint? bitrate = null;
				switch (Quality)
				{
					case (uint)TranscodeQuality.Low: 
						bitrate = 64;
						break;
					case (uint)TranscodeQuality.Medium: 
						bitrate = 96;	   
						break;
					case (uint)TranscodeQuality.High: 
						bitrate = 128;
						break;
					case (uint)TranscodeQuality.Extreme: 
						bitrate = 160;
						break;
					default: 
						bitrate = Quality;
						break;
				}
				return bitrate;
			}
		}
		
		private string NeroAacEncOptions(uint quality)
		{
			string theString = "-cbr " + quality;
			//Song song = new Song(Item.ItemId.Value);

			// Options for neroAacTag, needs to be chained in
			//theString += song.ArtistName == null ? String.Empty : " -meta:artist=\"" + song.ArtistName + "\"";
			//theString += song.AlbumName == null ? String.Empty : " -meta:album=\"" + song.AlbumName + "\"";
			//theString += song.SongName == null ? String.Empty : " -meta:title=\"" + song.SongName + "\"";
			//theString += song.ReleaseYear == null ? String.Empty : " -meta:date=\"" + song.ReleaseYear + "\"";

			theString += " -ignorelength -if - -of " + this.OutputPath;

			return theString;
		}
	}
}

