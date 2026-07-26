using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using TagLib;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Server.Extensions;
using WaveBox.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Core.Model.Repository;
using WaveBox.Core;
using System.Diagnostics;

namespace WaveBox.ApiHandler.Handlers {
    class ArtApiHandler : IApiHandler {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        public string Name { get { return "art"; } }

        // API handler is read-only, so no permissions checks needed
        public bool CheckPermission(User user, string action) {
            return true;
        }

        /// <summary>
        /// Process returns a file stream containing album art
        /// </summary>
        public void Process(UriWrapper uri, IHttpProcessor processor, User user) {
            // Check for the itemId
            if (uri.Id == null) {
                processor.WriteErrorHeader();
                return;
            }

            // Check for blur (value between 0 and 100)
            double blurSigma = 0;
            if (uri.Parameters.ContainsKey("blur")) {
                int blur = 0;
                Int32.TryParse(uri.Parameters["blur"], out blur);
                if (blur < 0) {
                    blur = 0;
                } else if (blur > 100) {
                    blur = 100;
                }

                blurSigma = (double)blur / 10.0;
            }

            // Grab art stream
            Art art = Injection.Get<IArtRepository>().ArtForId((int)uri.Id);
            Stream stream = ArtStream.CreateStream(art);

            // If the stream could not be produced, return error
            if ((object)stream == null) {
                processor.WriteErrorHeader();
                return;
            }

            // If art size requested...
            if (uri.Parameters.ContainsKey("size")) {
                int size = Int32.MaxValue;
                Int32.TryParse(uri.Parameters["size"], out size);

                // Parse size if valid
                if (size != Int32.MaxValue) {
                    try {
                        Stream resized = ArtStream.ResizeImage(stream, size, blurSigma);
                        stream.Close();
                        stream = resized;
                    } catch (Exception e) {
                        logger.Error("Error resizing art, returning original: ", e);
                        stream.Position = 0;
                    }
                }
            }

            DateTime? lastModified = null;
            if (!ReferenceEquals(art.LastModified, null)) {
                lastModified = ((long)art.LastModified).ToDateTime();
            }
            processor.WriteFile(stream, 0, stream.Length, HttpHeader.MimeTypeForExtension(".jpg"), null, true, lastModified);

            // Close the file so we don't get sharing violations on future accesses
            stream.Close();
        }
    }
}
