using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Service.Services.Http;
using WaveBox.Transcoding;

namespace WaveBox.Api {
    // Implements the legacy IHttpProcessor wire contract on top of an ASP.NET Core HttpContext,
    // so the existing API handlers run unchanged under Kestrel.
    public class HttpContextProcessor : IHttpProcessor {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(HttpContextProcessor));

        private readonly HttpContext context;

        public Hashtable HttpHeaders { get; set; }

        public ITranscoder Transcoder { get; set; }

        // Delayed headers, mostly used for updating sessions if needed
        public Dictionary<string, string> DelayedHeaders = new Dictionary<string, string>();

        public HttpContextProcessor(HttpContext context) {
            this.context = context;

            // Mirror request headers into the legacy Hashtable shape
            this.HttpHeaders = new Hashtable();
            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in context.Request.Headers) {
                this.HttpHeaders[header.Key] = header.Value.ToString();
            }
        }

        public void WriteNotModifiedHeader() {
            context.Response.StatusCode = 304;
        }

        public void WriteErrorHeader() {
            context.Response.StatusCode = 404;
        }

        public void WriteSuccessHeader(long contentLength, string mimeType, IDictionary<string, string> customHeaders, DateTime lastModified, bool isPartial = false, string encoding = null) {
            context.Response.StatusCode = isPartial ? 206 : 200;
            context.Response.Headers["Server"] = "WaveBox/" + ServerInfo.BuildVersion;
            context.Response.Headers["Last-Modified"] = lastModified.ToRFC1123();
            context.Response.Headers["ETag"] = "\"" + lastModified.ToETag() + "\"";
            context.Response.Headers["Accept-Ranges"] = "bytes";
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.ContentType = mimeType;

            if (contentLength >= 0) {
                context.Response.ContentLength = contentLength;
            }

            if ((object)customHeaders != null) {
                foreach (string key in customHeaders.Keys) {
                    context.Response.Headers[key] = customHeaders[key];
                }
            }

            // Inject delayed headers (session cookie refresh)
            foreach (string key in this.DelayedHeaders.Keys) {
                context.Response.Headers[key] = DelayedHeaders[key];
            }

            // Only log API responses
            if (context.Request.Path.StartsWithSegments("/api")) {
                logger.IfInfo(String.Format("HTTP {0}, Length: {1}, ETag: {2}, Last-Modified: {3}",
                                            context.Response.StatusCode,
                                            contentLength,
                                            lastModified.ToETag(),
                                            lastModified.ToRFC1123()
                                           ));
            }
        }

        public void WriteText(string text, string mimeType) {
            // Note: unlike the legacy server this writes no UTF-8 BOM and a correct Content-Length;
            // gzip/deflate is handled by the response compression middleware.
            byte[] data = Encoding.UTF8.GetBytes(text);
            this.WriteSuccessHeader(data.Length, mimeType + ";charset=utf-8", null, DateTime.UtcNow);

            try {
                context.Response.Body.Write(data, 0, data.Length);
            }
            // If write failure, client disconnected, so ignore and continue
            catch (IOException) {
            } catch (ObjectDisposedException) {
            } catch (Exception e) {
                logger.Error("Failed to write HTTP response");
                logger.Error(e);
            }
        }

        // Source-generated serializer contexts (NativeAOT-safe)
        private static readonly WaveBoxJsonContext compactJson = WaveBoxJsonContext.Default;
        private static readonly WaveBoxJsonContext indentedJson = new WaveBoxJsonContext(new JsonSerializerOptions {
            WriteIndented = true
        });

        // Write an API response out serialized as JSON
        public void WriteJson(IApiResponse api) {
            try {
                WaveBoxJsonContext context = Injection.Get<IServerSettings>().PrettyJson ? indentedJson : compactJson;
                // Serialize as the runtime type so each concrete response's full shape is written
                this.WriteText(JsonSerializer.Serialize(api, api.GetType(), context), "application/json");
            } catch (Exception e) {
                logger.Error(e);
            }
        }

        public void WriteFile(Stream fs, int startOffset, long length, string mimeType, IDictionary<string, string> customHeaders, bool isSendContentLength, DateTime? lastModified, long? limitToBytes = null) {
            if ((object)fs == null || !fs.CanRead || length == 0 || startOffset >= length) {
                return;
            }

            DateTime lastMod = CleanLastModified(lastModified);

            // If it exists, check to see if the headers contains an If-Modified-Since or If-None-Match entry
            if (this.HttpHeaders.ContainsKey("If-Modified-Since") && this.HttpHeaders["If-Modified-Since"].Equals(lastMod.ToRFC1123())) {
                this.WriteNotModifiedHeader();
                return;
            }
            if (this.HttpHeaders.ContainsKey("If-None-Match") && this.HttpHeaders["If-None-Match"].Equals(lastMod.ToETag())) {
                this.WriteNotModifiedHeader();
                return;
            }

            // Read/Write in 8 KB chunks
            const int chunkSize = 8192;

            byte[] buf = new byte[chunkSize];
            int bytesRead;
            long bytesWritten = 0;
            long totalBytesWritten = 0;
            Stream stream = context.Response.Body;
            long actualStartOffset = startOffset;

            if (fs.CanSeek) {
                // Seek to the start offset
                fs.Seek(startOffset, SeekOrigin.Begin);
                actualStartOffset = fs.Position;
                if (actualStartOffset < startOffset && !ReferenceEquals(Transcoder, null) && Transcoder.State == TranscodeState.Active) {
                    // Transcode file hasn't grown enough yet; wait for it to catch up
                    while (this.Transcoder.State == TranscodeState.Active) {
                        fs.Seek(startOffset, SeekOrigin.Begin);
                        actualStartOffset = fs.Position;
                        if (actualStartOffset >= startOffset) {
                            break;
                        }
                        Thread.Sleep(250);
                    }
                }

                totalBytesWritten = fs.Position;
            }

            long contentLength = length - actualStartOffset;
            if (!ReferenceEquals(limitToBytes, null) && contentLength > limitToBytes) {
                contentLength = (long)limitToBytes;
            }

            bool isPartial = startOffset != 0 || !ReferenceEquals(limitToBytes, null);
            if (isPartial) {
                if (ReferenceEquals(customHeaders, null)) {
                    customHeaders = new Dictionary<string, string>();
                }

                string contentRange = "bytes " + startOffset + "-" + (startOffset + contentLength - 1) + "/" + length;
                customHeaders["Content-Range"] = contentRange;
            }

            this.WriteSuccessHeader(isSendContentLength ? contentLength : -1, mimeType, customHeaders, lastMod, isPartial);
            logger.IfInfo("File header, contentLength: " + contentLength + ", contentType: " + mimeType);

            while (true) {
                try {
                    // Check for client disconnect so transcodes get cancelled
                    if (context.RequestAborted.IsCancellationRequested) {
                        break;
                    }

                    int thisChunkSize = chunkSize;
                    if (!ReferenceEquals(limitToBytes, null)) {
                        // Make sure we don't send too much data on the last (potentially) partial chunk
                        if (bytesWritten + chunkSize > limitToBytes) {
                            thisChunkSize = (int)(limitToBytes - bytesWritten);
                        }
                    }

                    // Attempt to read a chunk
                    bytesRead = fs.Read(buf, 0, thisChunkSize);

                    // Send the bytes out to the client
                    stream.Write(buf, 0, bytesRead);
                    stream.Flush();
                    bytesWritten += bytesRead;
                    totalBytesWritten += bytesRead;

                    // See if we need to stop the transfer to limit the size
                    if (!ReferenceEquals(limitToBytes, null) && bytesWritten == limitToBytes) {
                        break;
                    }

                    // See if we're done
                    if (bytesRead < chunkSize) {
                        // Check if the stream is done
                        if (!fs.CanSeek || !(fs is FileStream) || totalBytesWritten >= fs.Length) {
                            if ((object)this.Transcoder == null || Transcoder.State != TranscodeState.Active) {
                                break;
                            }
                        }

                        // Transcode still running; sleep for a bit to prevent a tight loop while the file grows
                        Thread.Sleep(250);
                    }
                } catch (IOException) {
                    // Client disconnected
                    break;
                } catch (ObjectDisposedException) {
                    break;
                } catch (Exception e) {
                    logger.Error("Failed to write file to HTTP response");
                    logger.Error(e);
                    break;
                }
            }
        }

        private DateTime CleanLastModified(DateTime? lastModified) {
            // If null, use current time
            if (ReferenceEquals(lastModified, null)) {
                return DateTime.UtcNow;
            }

            // Make sure we're using UTC
            DateTime lastMod = ((DateTime)lastModified).ToUniversalTime();

            // If the time is later than now, use now
            if (DateTime.Compare(DateTime.UtcNow, lastMod) < 0) {
                lastMod = DateTime.UtcNow;
            }

            return lastMod;
        }
    }
}
