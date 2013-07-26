using System;
using System.IO;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Static;
using Ninject;
using WaveBox.Core;

namespace WaveBox.Server.Extensions
{
	public static class IMediaItemExtensions
	{
		public static string FilePath(this IMediaItem mediaItem)
		{
			if (mediaItem.FolderId == null)
				return null;

			return Injection.Kernel.Get<IFolderRepository>().FolderForId((int)mediaItem.FolderId).FolderPath + Path.DirectorySeparatorChar + mediaItem.FileName;
		}

		public static FileStream File(this IMediaItem mediaItem)
		{ 
			return new FileStream(mediaItem.FilePath(), FileMode.Open, FileAccess.Read); 
		}
	}
}

