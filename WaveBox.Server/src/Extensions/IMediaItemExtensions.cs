using System;
using System.IO;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Static;

namespace WaveBox.Server.Extensions {
    public static class IMediaItemExtensions {
        /// <summary>
        /// Get the full filesystem path to this IMediaItem
        /// </summary>
        public static string FilePath(this IMediaItem mediaItem) {
            if (mediaItem.FolderId == null) {
                return null;
            }

            return Injection.Kernel.Get<IFolderRepository>().FolderForId((int)mediaItem.FolderId).FolderPath + Path.DirectorySeparatorChar + mediaItem.FileName;
        }

        /// <summary>
        /// Return a readonly FileStream of this IMediaItem
        /// </summary>
        public static FileStream File(this IMediaItem mediaItem) {
            return new FileStream(mediaItem.FilePath(), FileMode.Open, FileAccess.Read);
        }
    }
}
