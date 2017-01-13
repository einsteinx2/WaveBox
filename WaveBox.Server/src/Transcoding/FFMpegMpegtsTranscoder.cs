using System;
using WaveBox.Core.Model;
using WaveBox.Server.Extensions;
using WaveBox.Static;

namespace WaveBox.Transcoding {
    public class FFMpegMpegtsTranscoder : VideoTranscoder {
        public override TranscodeType Type { get { return TranscodeType.MPEGTS; } }

        public override string Codec { get { return "libx264"; } }

        public override string Command { get { return "ffmpeg"; } }

        public override string OutputExtension { get { return "ts"; } }

        public override string MimeType { get { return "video/MP2T"; } }

        public FFMpegMpegtsTranscoder(IMediaItem item, uint quality, bool isDirect, uint? width, uint? height, bool maintainAspect, uint offsetSeconds, uint lengthSeconds) : base(item, quality, isDirect, width, height, maintainAspect, offsetSeconds, lengthSeconds) {
        }

        protected override string GenerateArguments(uint abitrate, uint vbitrate, uint width, uint height) {
            return " -ss " + OffsetSeconds + " -t " + LengthSeconds + " -i \"" + Item.FilePath() + "\" -async 1 -b " + vbitrate + " -s " + width + "x" + height + " -ar 44100 -ab " + abitrate + " -ac 2 -v 0 -f mpegts -vcodec " + Codec + " -preset superfast -strict experimental -acodec aac -threads 0 " + OutputPath;
        }

        // Estimate high
        public override uint? EstimatedBitrate {
            get {
                if ((object)Quality == null) {
                    return null;
                }

                return (uint)((double)TotalBitrateForQuality(Quality) * 1.5);
            }
        }
    }
}
