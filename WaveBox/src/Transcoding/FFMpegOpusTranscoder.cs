using System;
using WaveBox.DataModel.Model;
using System.Diagnostics;
using System.IO;

namespace WaveBox.Transcoding
{
    public class FFMpegOpusTranscoder : AbstractTranscoder
    {
        public override TranscodeType Type { get { return TranscodeType.OPUS; } }
        
        public override string Command { get { return "ffmpeg"; } }
        
        public override string OutputExtension { get { return "opus"; } }
        
        public override string MimeType { get { return "audio/opus"; } }
        
        public FFMpegOpusTranscoder(IMediaItem item, uint quality, bool isDirect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, offsetSeconds, lengthSeconds)
        {
            
        }

        public override void Run()
        {
            try 
            {
                string ffmpegArguments = "-i \"" + Item.FilePath + "\" -f wav -";
                Console.WriteLine("[TRANSCODE] Forking the process");
                Console.WriteLine("[TRANSCODE] " + "ffmpeg " + ffmpegArguments);
                
                // Create the ffmpeg process
                var FfmpegProcess = new Process();
                FfmpegProcess.StartInfo.FileName = "ffmpeg";
                FfmpegProcess.StartInfo.Arguments = ffmpegArguments;
                FfmpegProcess.StartInfo.UseShellExecute = false;
                FfmpegProcess.StartInfo.RedirectStandardOutput = true;
                FfmpegProcess.StartInfo.RedirectStandardError = true;


                // Create the opusenc object
                TranscodeProcess = new Process();
                TranscodeProcess.StartInfo.FileName = "opusenc";
                TranscodeProcess.StartInfo.Arguments = ffmpegArguments;
                TranscodeProcess.StartInfo.UseShellExecute = false;
                TranscodeProcess.StartInfo.RedirectStandardInput = true;
                TranscodeProcess.StartInfo.RedirectStandardOutput = true;
                TranscodeProcess.StartInfo.RedirectStandardError = true;

                var buffer = new byte[512];
                FfmpegProcess.Start();
                TranscodeProcess.Start();

                var input = new BinaryWriter(TranscodeProcess.StandardInput.BaseStream);

                while(true)
                {
                    int bytesRead = FfmpegProcess.StandardOutput.BaseStream.Read(buffer, 0, 512);
                    input.Write(buffer, 0, bytesRead);

                    if(bytesRead < 512 && FfmpegProcess.HasExited)
                        break;
                }
                
                Console.WriteLine("[TRANSCODE] Waiting for processes to finish");
                
                // Block until done
                TranscodeProcess.WaitForExit();
                
                Console.WriteLine("[TRANSCODE] Process finished");
            }
            catch (Exception e) 
            {
                Console.WriteLine("\t" + "[TRANSCODE] Failed to start transcode process " + e);
                
                // Set the state
                State = TranscodeState.Failed;
                
                // Inform the delegate
                if ((object)TranscoderDelegate != null)
                    TranscoderDelegate.TranscodeFailed(this);
                
                return;
            }
            
            if (TranscodeProcess != null)
            {
                int exitValue = TranscodeProcess.ExitCode;
                Console.WriteLine("[TRANSCODE] exit value " + exitValue);
                
                if (exitValue == 0)
                {
                    State = TranscodeState.Finished;
                    
                    if ((object)TranscoderDelegate != null)
                        TranscoderDelegate.TranscodeFinished(this);
                }
                else
                {
                    State = TranscodeState.Failed;
                    
                    if ((object)TranscoderDelegate != null)
                        TranscoderDelegate.TranscodeFailed(this);
                }
            }
        }
        
        public override string Arguments
        {
            get 
            { 
                string codec = "libmp3lame";
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
            return "./opusenc --bitrate " + quality + " --vbr - " + " -ab " + OutputPath;
        }
    }
}

