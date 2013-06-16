using System;
using WaveBox.Model;
using System.IO;

namespace WaveBox.Server.Extensions
{
	public static class IMediaItemExtensions
	{
		public static string FilePath(this IMediaItem mediaItem)
		{
			return new Folder.Factory().CreateFolder((int)mediaItem.FolderId).FolderPath + Path.DirectorySeparatorChar + mediaItem.FileName;
		}

		public static FileStream File(this IMediaItem mediaItem)
		{ 
			return new FileStream(mediaItem.FilePath(), FileMode.Open, FileAccess.Read); 
		}
	}
}

