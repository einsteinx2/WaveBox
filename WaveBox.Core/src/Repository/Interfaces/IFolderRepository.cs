using System;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.Model.Repository
{
	public interface IFolderRepository
	{
		Folder FolderForId(int folderId);
		Folder FolderForPath(string path);
		IList<Folder> MediaFolders();
		IList<Folder> TopLevelFolders();
		IList<Song> ListOfSongs(int folderId, bool recursive = false);
		IList<Video> ListOfVideos(int folderId, bool recursive = false);
		IList<Folder> ListOfSubFolders(int folderId);
		int? GetParentFolderId(string path);
	}
}

