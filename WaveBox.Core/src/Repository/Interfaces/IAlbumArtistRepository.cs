using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IAlbumArtistRepository
	{
		AlbumArtist AlbumArtistForId(int? albumArtistId);
		AlbumArtist AlbumArtistForName(string albumArtistName);
		bool InsertAlbumArtist(string albumArtistName);
		AlbumArtist AlbumArtistForNameOrCreate(string albumArtistName);
		IList<AlbumArtist> AllAlbumArtists();
		int CountAlbumArtists();
		IList<AlbumArtist> SearchAlbumArtists(string field, string query, bool exact = true);
		IList<AlbumArtist> RangeAlbumArtists(char start, char end);
		IList<AlbumArtist> LimitAlbumArtists(int index, int duration = Int32.MinValue);
	}
}
