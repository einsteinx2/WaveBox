using System;
using WaveBox.Core.Model;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IAlbumRepository
	{
		Album AlbumForId(int albumId);
		Album AlbumForName(string albumName, int? artistId);
		bool InsertAlbum(string albumName, int? artistId, int? albumArtistId, int? releaseYear);
		Album AlbumForName(string albumName, int? artistId, int? albumArtistId, int? releaseYear = null);
		IList<Album> AllAlbums();
		int CountAlbums();
		IList<Album> SearchAlbums(string field, string query, bool exact = true);
		IList<Album> RandomAlbums(int limit = 10);
		IList<Album> RangeAlbums(char start, char end);
		IList<Album> LimitAlbums(int index, int duration = Int32.MinValue);
		IList<int> SongArtIds(int albumId);
		IList<int> FolderArtIds(int albumId);
	}
}

