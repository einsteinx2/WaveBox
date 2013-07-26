using System;
using WaveBox.Core.Model;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IAlbumRepository
	{
		Album AlbumForId(int albumId);
		Album AlbumForName(string albumName, int? artistId);
		bool InsertAlbum(string albumName, int? artistId, int? releaseYear);
		Album AlbumForName(string albumName, int? artistId, int? releaseYear = null);
		List<Album> AllAlbums();
		int CountAlbums();
		List<Album> SearchAlbums(string field, string query, bool exact = true);
		List<Album> RandomAlbums(int limit = 10);
		List<Album> RangeAlbums(char start, char end);
		List<Album> LimitAlbums(int index, int duration = Int32.MinValue);
		List<int> SongArtIds(int albumId);
		List<int> FolderArtIds(int albumId);
	}
}

