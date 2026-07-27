using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Server.Extensions;

namespace WaveBox.ApiHandler {
    // Art resolution and resizing, shared by the legacy /api/art handler and Subsonic getCoverArt.
    // Extracted verbatim from ArtApiHandler: art bytes are not stored in the database — they are
    // re-read from the song's tag or the folder's image file on every request.
    public static class ArtStream {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(ArtStream));

        public static Stream CreateStream(Art art) {
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

        /// <summary>
        /// Aspect-fit resize into a size x size box (Lanczos), optional Gaussian blur, always re-encoded as JPEG
        /// </summary>
        public static Stream ResizeImage(Stream stream, int size, double blurSigma) {
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

        public static Stream StreamForSong(int songId) {
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

        public static Stream StreamForFolder(int folderId) {
            Folder folder = Injection.Get<IFolderRepository>().FolderForId(folderId);
            Stream stream = null;

            string artPath = FolderArtPath(folder);

            if ((object)artPath != null) {
                stream = new FileStream(artPath, FileMode.Open, FileAccess.Read);
            }

            return stream;
        }

        public static string FolderArtPath(Folder folder) {
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

        public static bool FolderContainsImages(string dir, out string firstImageFoundPath) {
            string[] validImageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            string ext = null;
            firstImageFoundPath = null;

            foreach (string file in Directory.GetFiles(dir)) {
                ext = Path.GetExtension(file).ToLower();
                if (validImageExtensions.Contains(ext) && !Path.GetFileName(file).StartsWith('.')) {
                    firstImageFoundPath = file;
                }
            }

            // Return true if firstImageFoundPath exists
            return ((object)firstImageFoundPath != null);
        }
    }
}
