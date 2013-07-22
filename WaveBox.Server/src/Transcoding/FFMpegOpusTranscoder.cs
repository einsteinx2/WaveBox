using System;
using System.Diagnostics;
using System.IO;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.Server.Extensions;
using Ninject;
using WaveBox.Model.Repository;

namespace WaveBox.Transcoding
{
	public class FFMpegOpusTranscoder : AbstractTranscoder
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override TranscodeType Type { get { return TranscodeType.OPUS; } }

		// Placeholder for ffmpeg opus support
		public override string Codec { get { return "libopus"; } }

		public override string Command { get { return "ffmpeg"; } }

		public override string OutputExtension { get { return "opus"; } }

		public override string MimeType { get { return "audio/opus"; } }

		private Process FfmpegProcess;

		public FFMpegOpusTranscoder(IMediaItem item, uint quality, bool isDirect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, offsetSeconds, lengthSeconds)
		{
		}

		public override void Run()
		{
			try 
			{
				string ffmpegArguments = "-loglevel quiet -i \"" + Item.FilePath() + "\" -f wav -";
				if (logger.IsInfoEnabled) logger.Info("Forking the process");
				if (logger.IsInfoEnabled) logger.Info("ffmpeg " + ffmpegArguments);

				// Create the ffmpeg process
				FfmpegProcess = new Process();
				FfmpegProcess.StartInfo.FileName = "ffmpeg";
				FfmpegProcess.StartInfo.Arguments = ffmpegArguments;
				FfmpegProcess.StartInfo.UseShellExecute = false;
				FfmpegProcess.StartInfo.RedirectStandardOutput = true;
				FfmpegProcess.StartInfo.RedirectStandardError = true;

				// Create the opusenc object
				if (logger.IsInfoEnabled) logger.Info("opusenc " + Arguments);
				TranscodeProcess = new Process();
				TranscodeProcess.StartInfo.FileName = "opusenc";
				TranscodeProcess.StartInfo.Arguments = Arguments;
				TranscodeProcess.StartInfo.UseShellExecute = false;
				TranscodeProcess.StartInfo.RedirectStandardInput = true;
				TranscodeProcess.StartInfo.RedirectStandardOutput = true;
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

					if (bytesRead == 0 && FfmpegProcess.HasExited)
					{
						input.Close();
						FfmpegProcess.Close();
						break;
					}
				}

				if (logger.IsInfoEnabled) logger.Info("Waiting for processes to finish");

				// Block until done
				TranscodeProcess.WaitForExit();

				if (logger.IsInfoEnabled) logger.Info("Process finished");
			}
			catch (Exception e) 
			{
				if (logger.IsInfoEnabled) logger.Info("\t" + "Failed to start transcode process " + e);

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
				if (logger.IsInfoEnabled) logger.Info("Exit value " + exitValue);

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
				string codec = "libopus";
				string options = null;
				switch (Quality)
				{
					case (uint)TranscodeQuality.Low:
						// VBR - V9 quality (~64 kbps)
						options = OpusencOptions(codec, 64);
						break;
					case (uint)TranscodeQuality.Medium:
						// VBR - V5 quality (~128 kbps)
						options = OpusencOptions(codec, 96);
						break;
					case (uint)TranscodeQuality.High:
						// VBR - V2 quality (~192 kbps)
						options = OpusencOptions(codec, 128);
						break;
					case (uint)TranscodeQuality.Extreme:
						// VBR - V0 quality (~224 kbps)
						options = OpusencOptions(codec, 160);
						break;
					default:
						options = OpusencOptions(codec, Quality);
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
		
		private string OpusencOptions(String codec, uint quality)
		{
			string theString = "--bitrate " + quality;
			theString += " --quiet";
			Song song = Injection.Kernel.Get<ISongRepository>().SongForId(Item.ItemId.Value);

			theString += song.ArtistName == null ? String.Empty : " --comment ARTIST=\"" + song.ArtistName + "\"";
			theString += song.AlbumName == null ? String.Empty : " --comment ALBUM=\"" + song.AlbumName + "\"";
			theString += song.SongName == null ? String.Empty : " --comment TITLE=\"" + song.SongName + "\"";
			theString += song.ReleaseYear == null ? String.Empty : " --comment DATE=\"" + song.ReleaseYear + "\"";

			theString += " --vbr - " + OutputPath;

			return theString;
		}
	}
}
