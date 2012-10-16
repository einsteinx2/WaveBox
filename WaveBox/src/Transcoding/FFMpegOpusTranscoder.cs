using System;
using WaveBox.DataModel.Model;
using System.Diagnostics;

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
                    options = FFMpegOptionsWith(codec, 64);
                    break;
                case (uint)TranscodeQuality.Medium:
                    // VBR - V5 quality (~128 kbps)
                    options = FFMpegOptionsWith(codec, 96);
                    break;
                case (uint)TranscodeQuality.High:
                    // VBR - V2 quality (~192 kbps)
                    options = FFMpegOptionsWith(codec, 128);
                    break;
                case (uint)TranscodeQuality.Extreme:
                    // VBR - V0 quality (~224 kbps)
                    options = FFMpegOptionsWith(codec, 160);
                    break;
                default:
                    options = FFMpegOptionsWith(codec, Quality);
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
        
        private string FFMpegOptionsWith(String codec, uint quality)
        {
            return "-i \"" + Item.FilePath + "\" -f wav - | ./opusenc --bitrate " + quality + " --vbr - " + " -ab " + OutputPath;
        }
    }
}

