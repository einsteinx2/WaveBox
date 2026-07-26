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
            Stream stream = CreateStream(art);

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
                        Stream resized = ResizeImage(stream, size, blurSigma);
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

        /// <summary>
        /// Aspect-fit resize into a size x size box (Lanczos), optional Gaussian blur, always re-encoded as JPEG
        /// </summary>
        private Stream ResizeImage(Stream stream, int size, double blurSigma) {
            using (var image = SixLabors.ImageSharp.Image.Load(stream)) {
                float nPercentW = ((float)size / (float)image.Width);
                float nPercentH = ((float)size / (float)image.Height);
                float nPercent = nPercentH < nPercentW ? nPercentH : nPercentW;

                int destWidth = (int)(image.Width * nPercent);
                int destHeight = (int)(image.Height * nPercent);

                image.Mutate(x => {
                    x.Resize(destWidth, destHeight, KnownResamplers.Lanczos3);
                    if (blurSigma > 0.0) {
                        x.GaussianBlur((float)blurSigma);
                    }
                });

                MemoryStream output = new MemoryStream();
                image.SaveAsJpeg(output);
                output.Position = 0;
                return output;
            }
        }

        private Stream CreateStream(Art art) {
            if ((object)art.ArtId == null) {
                return null;
            }

            int? itemId = Injection.Get<IArtRepository>().ItemIdForArtId((int)art.ArtId);

            if ((object)itemId == null) {
                return null;
            }

            ItemType type = Injection.Get<IItemRepository>().ItemTypeForItemId((int)itemId);

            Stream stream = null;

            if (type == ItemType.Song) {
                stream = StreamForSong((int)itemId);
            } else if (type == ItemType.Folder) {
                stream = StreamForFolder((int)itemId);
            }

            return stream;
        }

        private Stream StreamForSong(int songId) {
            Song song = Injection.Get<ISongRepository>().SongForId(songId);
            Stream stream = null;

            // Open the image from the tag
            TagLib.File f = null;
            try {
                f = TagLib.File.Create(song.FilePath());
                byte[] data = f.Tag.Pictures[0].Data.Data;

                stream = new MemoryStream(data);
            } catch (TagLib.CorruptFileException e) {
                logger.IfInfo(song.FileName + " has a corrupt tag so can't return the art. " + e);
            } catch (Exception e) {
                logger.Error("Error processing file: ", e);
            }

            return stream;
        }

        private Stream StreamForFolder(int folderId) {
            Folder folder = Injection.Get<IFolderRepository>().FolderForId(folderId);
            Stream stream = null;

            string artPath = FolderArtPath(folder);

            if ((object)artPath != null) {
                stream = new FileStream(artPath, FileMode.Open, FileAccess.Read);
            }

            return stream;
        }

        private string FolderArtPath(Folder folder) {
            string artPath = null;

            foreach (string fileName in Injection.Get<IServerSettings>().FolderArtNames) {
                string path = folder.FolderPath + Path.DirectorySeparatorChar + fileName;
                if (System.IO.File.Exists(path)) {
                    // Use this one
                    artPath = path;
                }
            }

            if ((object)artPath == null) {
                // Check for any images
                FolderContainsImages(folder.FolderPath, out artPath);
            }

            return artPath;
        }

        private bool FolderContainsImages(string dir, out string firstImageFoundPath) {
            string[] validImageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            string ext = null;
            firstImageFoundPath = null;

            foreach (string file in Directory.GetFiles(dir)) {
                ext = Path.GetExtension(file).ToLower();
                if (validImageExtensions.Contains(ext) && !Path.GetFileName(file).StartsWith(".")) {
                    firstImageFoundPath = file;
                }
            }

            // Return true if firstImageFoundPath exists
            return ((object)firstImageFoundPath != null);
        }
    }
}
