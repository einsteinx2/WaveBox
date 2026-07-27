using System;
using System.IO;
using WaveBox.Api;
using WaveBox.ApiHandler;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Server.Extensions;
using WaveBox.Service;
using WaveBox.Service.Services;
using WaveBox.Service.Services.Http;
using WaveBox.Transcoding;

namespace WaveBox.Subsonic.Handlers {
    public static class SubsonicMediaHandlers {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(SubsonicMediaHandlers));

        public static void GetCoverArt(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int? id = req.GetInt("id");
            if (id == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Cover art not found");
                return;
            }

            Art art = Injection.Get<IArtRepository>().ArtForId((int)id);
            Stream stream = ArtStream.CreateStream(art);
            if ((object)stream == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Cover art not found");
                return;
            }

            int? size = req.GetInt("size");
            if (size != null && size > 0) {
                try {
                    Stream resized = ArtStream.ResizeImage(stream, (int)size, 0);
                    stream.Close();
                    stream = resized;
                } catch (Exception e) {
                    logger.Error("Error resizing cover art, returning original: ", e);
                    stream.Position = 0;
                }
            }

            DateTime? lastModified = null;
            if (art.LastModified != null) {
                lastModified = ((long)art.LastModified).ToDateTime();
            }
            processor.WriteFile(stream, 0, stream.Length, "image/jpeg", null, true, lastModified);
            stream.Close();
        }

        public static void Stream(SubsonicRequest req, HttpContextProcessor processor, User user) {
            StreamOrDownload(req, processor, allowTranscode: true);
        }

        public static void Download(SubsonicRequest req, HttpContextProcessor processor, User user) {
            StreamOrDownload(req, processor, allowTranscode: false);
        }

        private static void StreamOrDownload(SubsonicRequest req, HttpContextProcessor processor, bool allowTranscode) {
            int? id = req.GetInt("id");
            if (id == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            IMediaItem item = null;
            ItemType itemType = Injection.Get<IItemRepository>().ItemTypeForItemId((int)id);
            if (itemType == ItemType.Song) {
                item = Injection.Get<ISongRepository>().SongForId((int)id);
            } else if (itemType == ItemType.Video) {
                item = Injection.Get<IVideoRepository>().VideoForId((int)id);
            }

            if (item == null || item.ItemId == null || !File.Exists(item.FilePath())) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Media not found");
                return;
            }

            string format = req.Get("format");
            int maxBitRate = req.GetInt("maxBitRate") ?? 0;

            if (allowTranscode && itemType == ItemType.Song && !"raw".Equals(format, StringComparison.OrdinalIgnoreCase)) {
                string nativeSuffix = Path.GetExtension(item.FileName ?? "").TrimStart('.').ToLowerInvariant();
                bool wantsFormat = !String.IsNullOrEmpty(format) && !format.Equals(nativeSuffix, StringComparison.OrdinalIgnoreCase);
                bool overBitrate = maxBitRate > 0 && (item.Bitrate == null || item.Bitrate > maxBitRate);

                if (wantsFormat || overBitrate) {
                    TranscodeSong(req, processor, item, format, maxBitRate);
                    return;
                }
            }

            SendDirect(req, processor, item);
        }

        // Direct byte-for-byte file streaming with Range support (same semantics as /api/stream)
        private static void SendDirect(SubsonicRequest req, HttpContextProcessor processor, IMediaItem item) {
            try {
                System.IO.FileStream stream = item.File();
                long length = stream.Length;
                int startOffset = 0;
                long? limitToSize = null;

                if (processor.HttpHeaders.ContainsKey("Range")) {
                    string range = (string)processor.HttpHeaders["Range"];
                    var split = range.Split('-', '=');
                    string start = split[1];
                    string end = split.Length > 2 ? split[2] : null;

                    logger.IfInfo("Range header: " + range + "  Resuming from " + start);
                    startOffset = Convert.ToInt32(start);
                    if (!ReferenceEquals(end, null) && end != String.Empty) {
                        limitToSize = (Convert.ToInt64(end) + 1) - startOffset;
                    }
                }

                processor.WriteFile(stream, startOffset, length, item.FileType.MimeType(), null, true, new FileInfo(item.FilePath()).LastWriteTimeUtc, limitToSize);
                stream.Close();
            } catch (Exception e) {
                logger.Error(e);
            }
        }

        private static void TranscodeSong(SubsonicRequest req, HttpContextProcessor processor, IMediaItem item, string format, int maxBitRate) {
            TranscodeService transcodeService = (TranscodeService)ServiceManager.GetInstance("transcode");
            if ((object)transcodeService == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "Transcoding is not available");
                return;
            }

            TranscodeType transType = TranscodeTypeForFormat(format);

            // A quality value above the TranscodeQuality enum range is treated as raw kbps
            uint quality = maxBitRate > 0 ? (uint)maxBitRate : 192;

            logger.IfInfo("Subsonic transcode: " + item.FileName + " -> " + transType + " @ " + quality);

            ITranscoder transcoder = transcodeService.TranscodeSong(item, transType, quality, false, 0, (uint)(item.Duration ?? 0));
            bool estimateContentLength = req.GetBool("estimateContentLength", false);

            TranscodeStreamer.Send(transcodeService, transcoder, processor, 0, null, estimateContentLength);
        }

        internal static TranscodeType TranscodeTypeForFormat(string format) {
            switch ((format ?? "mp3").ToLowerInvariant()) {
            case "aac":
            case "m4a":
            case "mp4":
                return TranscodeType.AAC;
            case "ogg":
            case "oga":
                return TranscodeType.OGG;
            case "opus":
                return TranscodeType.OPUS;
            default:
                return TranscodeType.MP3;
            }
        }
    }
}
