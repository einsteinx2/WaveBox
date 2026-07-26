using System;
using System.IO;
using System.Threading;
using WaveBox.Core.Extensions;
using WaveBox.Service.Services;
using WaveBox.Service.Services.Http;

namespace WaveBox.Transcoding {
    // The wait-for-transcode-output-and-send loop, shared by the legacy /api/transcode handler
    // and Subsonic stream.  Extracted verbatim from TranscodeApiHandler.
    public static class TranscodeStreamer {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(TranscodeStreamer));

        // Waits up to 5 seconds for the transcoder's output (file or stdout stream) to appear,
        // streams it to the client (tailing the growing file via processor.Transcoder), then
        // spins off the consume thread that reference-counts and eventually cleans up the
        // transcoder.  Returns false if no output ever appeared (an error header was written).
        public static bool Send(TranscodeService transcodeService, ITranscoder transcoder, IHttpProcessor processor, int startOffset, long? limitToSize, bool estimateContentLength) {
            if ((object)transcoder == null) {
                processor.WriteErrorHeader();
                return false;
            }

            Stream stream = null;
            long length = (long)transcoder.EstimatedOutputSize;

            // Wait up 5 seconds for file or basestream to appear
            for (int i = 0; i < 20; i++) {
                if (transcoder.IsDirect) {
                    logger.IfInfo("Checking if base stream exists");
                    if ((object)transcoder.TranscodeProcess != null && (object)transcoder.TranscodeProcess.StandardOutput.BaseStream != null) {
                        // The base stream exists, so the transcoding process has started
                        logger.IfInfo("Base stream exists, starting transfer");
                        stream = transcoder.TranscodeProcess.StandardOutput.BaseStream;
                        break;
                    }
                } else {
                    logger.IfInfo("Checking if file exists (" + transcoder.OutputPath + ")");
                    if (File.Exists(transcoder.OutputPath)) {
                        // The file exists, so the transcoding process has started
                        stream = new FileStream(transcoder.OutputPath, FileMode.Open, FileAccess.Read);
                        break;
                    }
                }
                Thread.Sleep(250);
            }

            bool sent = false;
            if ((transcoder.IsDirect && (object)stream != null) ||
                    (!transcoder.IsDirect && File.Exists(transcoder.OutputPath))) {
                processor.Transcoder = transcoder;

                DateTime lastModified = transcoder.IsDirect ? DateTime.UtcNow : new FileInfo(transcoder.OutputPath).LastWriteTimeUtc;

                processor.WriteFile(stream, startOffset, length, transcoder.MimeType, null, estimateContentLength, lastModified, limitToSize);
                stream.Close();
                sent = true;
            } else {
                processor.WriteErrorHeader();
            }

            // Spin off a thread to consume the transcoder in 30 seconds.
            Thread consume = new Thread(() => transcodeService.ConsumedTranscode(transcoder));
            consume.Start();

            return sent;
        }
    }
}
