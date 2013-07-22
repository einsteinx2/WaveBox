using System;
using System.Collections.Generic;
using WaveBox.Model;

namespace WaveBox.Model.Repository
{
	public interface IFolderRepository
	{
		Folder FolderForId(int folderId);
		Folder FolderForPath(string path);
		List<Folder> MediaFolders();
		List<Folder> TopLevelFolders();
		List<Song> ListOfSongs(int folderId, bool recursive = false);
		List<Video> ListOfVideos(int folderId, bool recursive = false);
		List<Folder> ListOfSubFolders(int folderId);
		int? GetParentFolderId(string path);
	}
}

