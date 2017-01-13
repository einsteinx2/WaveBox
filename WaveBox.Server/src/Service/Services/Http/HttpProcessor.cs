using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Ninject;
using WaveBox;
using WaveBox.ApiHandler;
using WaveBox.ApiHandler.Handlers;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using WaveBox.Static;
using WaveBox.Transcoding;

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske.

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/

namespace WaveBox.Service.Services.Http {
    public partial class HttpProcessor : IHttpProcessor {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TcpClient Socket { get; set; }

        private Stream InputStream { get; set; }

        public String HttpMethod { get; set; }
        public String HttpUrl { get; set; }
        public String HttpProtocolVersionString { get; set; }
        public Hashtable HttpHeaders { get; set; }

        public ITranscoder Transcoder { get; set; }

        // Delayed headers, mostly used for updating sessions if needed
        public Dictionary<string, string> DelayedHeaders = new Dictionary<string, string>();

        private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

        public HttpProcessor(TcpClient s) {
            HttpHeaders = new Hashtable();
            Socket = s;
        }

        public void Process() {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            this.InputStream = this.Socket.GetStream();

            // we probably shouldn't be using a streamwriter for all output from handlers either
            try {
                this.InputStream.ReadTimeout = 30000;

                // Read in first line of request, get tokens for HTTP method, URL, version
                this.ParseRequest();

                // Captures hashtable of HTTP headers sent with request
                this.ReadHeaders();

                // GET, DELETE, PUT - parameters passed in query string
                if (this.HttpMethod == "GET" || this.HttpMethod == "DELETE" || this.HttpMethod == "PUT") {
                    this.HandleGETRequest();
                }
                // POST - parameters passed in HTTP headers
                else if (this.HttpMethod == "POST") {
                    this.HandlePOSTRequest();
                } else {
                    // HTTP 405: Unsupported method
                    this.WriteMethodNotAllowedHeader();
                }
            } catch (Exception e) {
                logger.Error("Exception occurred during HTTP processing");
                logger.Error(e);
                this.WriteInternalServerErrorHeader();
            } finally {
                // Ensure all streams and sockets are closed
                this.InputStream = null;
                this.Socket.GetStream().Close();
                this.Socket.Client.Close();
                this.Socket.Close();
            }
        }

        public void WriteNotModifiedHeader() {
            StreamWriter outStream = new StreamWriter(new BufferedStream(this.Socket.GetStream()));
            outStream.WriteLine("HTTP/1.1 304 Not Modified");
            outStream.WriteLine("Connection: close");
            outStream.WriteLine("");
            outStream.Flush();
        }

        public void WriteErrorHeader() {
            StreamWriter outStream = new StreamWriter(new BufferedStream(this.Socket.GetStream()));
            outStream.WriteLine("HTTP/1.1 404 Not Found");
            outStream.WriteLine("Connection: close");
            outStream.WriteLine("");
            outStream.Flush();
        }

        public void WriteMethodNotAllowedHeader() {
            StreamWriter outStream = new StreamWriter(new BufferedStream(this.Socket.GetStream()));
            outStream.WriteLine("HTTP/1.1 405 Method Not Allowed");
            outStream.WriteLine("Allow: GET, POST");
            outStream.WriteLine("Connection: close");
            outStream.WriteLine("");
            outStream.Flush();
        }

        public void WriteInternalServerErrorHeader() {
            StreamWriter outStream = new StreamWriter(new BufferedStream(this.Socket.GetStream()));
            outStream.WriteLine("HTTP/1.1 500 Internal Server Error");
            outStream.WriteLine("Connection: close");
            outStream.WriteLine("");
            outStream.Flush();
        }

        public void WriteSuccessHeader(long contentLength, string mimeType, IDictionary<string, string> customHeaders, DateTime lastModified, bool isPartial = false, string encoding = null) {
            StreamWriter outStream = new StreamWriter(new BufferedStream(this.Socket.GetStream()));
            string status = isPartial ? "HTTP/1.1 206 Partial Content" : "HTTP/1.1 200 OK";
            outStream.WriteLine(status);
            outStream.WriteLine("Date: " + DateTime.UtcNow.ToRFC1123());
            outStream.WriteLine("Server: WaveBox/" + WaveBoxService.BuildVersion);
            outStream.WriteLine("Last-Modified: " + lastModified.ToRFC1123());
            outStream.WriteLine("ETag: \"" + lastModified.ToETag() + "\"");
            outStream.WriteLine("Accept-Ranges: bytes");

            // Check request for compression
            if (encoding != null) {
                outStream.WriteLine("Content-Encoding: " + encoding);
            }

            if (contentLength >= 0) {
                outStream.WriteLine("Content-Length: " + contentLength);
            }

            outStream.WriteLine("Access-Control-Allow-Origin: *");
            outStream.WriteLine("Content-Type: " + mimeType);

            if ((object)customHeaders != null) {
                foreach (string key in customHeaders.Keys) {
                    outStream.WriteLine(key + ": " + customHeaders[key]);
                }
            }

            // Inject delayed headers
            foreach (string key in this.DelayedHeaders.Keys) {
                outStream.WriteLine(key + ": " + DelayedHeaders[key]);
            }

            outStream.WriteLine("Connection: close");
            outStream.WriteLine("");
            outStream.Flush();

            // Only log API responses
            if (HttpUrl.Contains("api")) {
                logger.IfInfo(String.Format("{0}, Length: {1}, Encoding: {2}, ETag: {3}, Last-Modified: {4}",
                                            status,
                                            contentLength,
                                            encoding ?? "none",
                                            lastModified.ToETag(),
                                            lastModified.ToRFC1123()
                                           ));
            }
        }

        public void WriteCompressedText(byte[] input, string mimeType, string encoding) {
            try {
                byte[] output = null;

                // Create a MemoryStream for compression
                using (MemoryStream memStream = new MemoryStream()) {
                    Stream zipStream = null;

                    // Attempt GZIP compression
                    if (encoding == "gzip") {
                        zipStream = new GZipStream(memStream, CompressionMode.Compress);
                    }
                    // Attempt DEFLATE compression
                    else if (encoding == "deflate") {
                        zipStream = new DeflateStream(memStream, CompressionMode.Compress);
                    } else {
                        logger.Error("Unknown encoding: " + encoding);
                        return;
                    }

                    // Write compressed data to stream
                    zipStream.Write(input, 0, input.Length);
                    zipStream.Flush();
                    zipStream.Dispose();

                    // Grab compressed output from memory
                    output = memStream.ToArray();
                }

                // Compression okay, write success header
                this.WriteSuccessHeader(output.Length, mimeType + ";charset=utf-8", null, DateTime.UtcNow, false, encoding);

                // Write the stream
                var binStream = new BinaryWriter(new BufferedStream(this.Socket.GetStream()), Encoding.UTF8);
                binStream.Write(output);
                binStream.Flush();
            }
            // If write failure, client disconnected, so ignore and continue
            catch (IOException) {
            } catch (Exception e) {
                logger.Error("Failed to write compressed HTTP response: " + encoding);
                logger.Error(e);
            }

            return;
        }

        public void WriteText(string text, string mimeType) {
            // If compression requested, attempt to send compressed
            if (this.HttpHeaders.ContainsKey("Accept-Encoding")) {
                // Check which encoding
                string accepted = this.HttpHeaders["Accept-Encoding"].ToString();
                string encoding = null;
                if (accepted.Contains("gzip")) {
                    encoding = "gzip";
                } else if (accepted.Contains("deflate")) {
                    encoding = "deflate";
                }

                // Bad encoding, send plaintext
                if (encoding != null) {
                    // Send compressed stream if valid encoding
                    byte[] input = Encoding.UTF8.GetBytes(text);
                    this.WriteCompressedText(input, mimeType, encoding);
                    return;
                }
            }

            // Makes no sense at all, but for whatever reason, all ajax calls fail with a cross site
            // scripting error if Content-Type is set, but the player needs it for files for seeking,
            // so pass -1 for no Content-Length header for all text requests
            this.WriteSuccessHeader(Encoding.UTF8.GetByteCount(text) + 3, mimeType + ";charset=utf-8", null, DateTime.UtcNow);

            try {
                StreamWriter outStream = new StreamWriter(new BufferedStream(this.Socket.GetStream()), Encoding.UTF8);
                outStream.Write(text);
                outStream.Flush();
            }
            // If write failure, client disconnected, so ignore and continue
            catch (IOException) {
            } catch (Exception e) {
                logger.Error("Failed to write HTTP response");
                logger.Error(e);
            }
        }

        // Write an API response out serialized as JSON
        public void WriteJson(IApiResponse api) {
            try {
                this.WriteText(JsonConvert.SerializeObject(api, Injection.Kernel.Get<IServerSettings>().JsonFormatting), "application/json");
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

            // Initialize everything
            byte[] buf = new byte[chunkSize];
            int bytesRead;
            long bytesWritten = 0;
            long totalBytesWritten = 0;
            this.Socket.SendTimeout = 30000;
            Stream stream = new BufferedStream(this.Socket.GetStream());
            int sinceLastReport = 0;
            long actualStartOffset = startOffset;
            Stopwatch sw = new Stopwatch();

            if (fs.CanSeek) {
                // Seek to the start offset
                fs.Seek(startOffset, SeekOrigin.Begin);
                actualStartOffset = fs.Position;
                if (actualStartOffset < startOffset && !ReferenceEquals(Transcoder, null) && Transcoder.State == TranscodeState.Active) {
                    // Wait for the file to catch up
                    while (this.Transcoder.State == TranscodeState.Active) {
                        // Try the seek again
                        fs.Seek(startOffset, SeekOrigin.Begin);

                        // Check the position
                        actualStartOffset = fs.Position;
                        if (actualStartOffset >= startOffset) {
                            // We've made it, so break
                            break;
                        }

                        // Sleep for a bit to prevent a tight loop
                        Thread.Sleep(250);
                    }
                }

                totalBytesWritten = fs.Position;
            }

            // TODO: make sure content length is correct when doing range requests on transcoded files
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

            sw.Start();
            while (true) {
                try {
                    int thisChunkSize = chunkSize;
                    if (!ReferenceEquals(limitToBytes, null)) {
                        // Make sure we don't send too much data on the last (potentially) partial chunk
                        if (bytesWritten + chunkSize > limitToBytes) {
                            // Reduce the chunk size
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

                    // Log the progress (only for testing)
                    if (sw.ElapsedMilliseconds > 1000) {
                        if (logger.IsInfoEnabled) {
                            logger.IfInfo(String.Format("[ {0,10} / {1,10} | {2:000}% | {3:00.00000} Mbps ]",
                                                        totalBytesWritten,
                                                        (contentLength + startOffset),
                                                        ((Convert.ToDouble(totalBytesWritten) / Convert.ToDouble(contentLength + startOffset)) * 100),
                                                        Math.Round((((double)(sinceLastReport * 8) / 1024) / 1024) / (double)(sw.ElapsedMilliseconds / 1000), 5)
                                                       ));
                        }

                        sinceLastReport = 0;
                        sw.Restart();
                    } else {
                        sinceLastReport += bytesRead;
                    }

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

                        // Sleep for a bit to prevent a tight loop
                        Thread.Sleep(250);
                    }
                } catch (IOException e) {
                    if (e.InnerException.GetType() == typeof(System.Net.Sockets.SocketException)) {
                        SocketException se = (SocketException)e.InnerException;
                        if (se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset) {
                            logger.IfInfo("Connection was forcibly closed by the remote host");
                        }
                    }

                    // Break the loop on error
                    break;
                }
            }

            sw.Stop();
        }
    }
}
