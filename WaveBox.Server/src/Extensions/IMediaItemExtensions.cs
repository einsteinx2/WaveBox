using System;
using System.IO;
using WaveBox.Model;
using WaveBox.Model.Repository;
using WaveBox.Static;
using Ninject;

namespace WaveBox.Server.Extensions
{
	public static class IMediaItemExtensions
	{
		public static string FilePath(this IMediaItem mediaItem)
		{
			return Injection.Kernel.Get<IFolderRepository>().FolderForId((int)mediaItem.FolderId).FolderPath + Path.DirectorySeparatorChar + mediaItem.FileName;
		}

		public static FileStream File(this IMediaItem mediaItem)
		{ 
			return new FileStream(mediaItem.FilePath(), FileMode.Open, FileAccess.Read); 
		}
	}
}

