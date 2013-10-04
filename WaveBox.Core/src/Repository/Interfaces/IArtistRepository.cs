using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IArtistRepository
	{
		Artist ArtistForId(int? artistId);
		Artist ArtistForName(string artistName);
		bool InsertArtist(string artistName, bool replace = false);
		bool InsertArtist(Artist artist, bool replace = false);
		Artist ArtistForNameOrCreate(string artistName);
		IList<Artist> AllArtists();
		int CountArtists();
		IList<Artist> SearchArtists(string field, string query, bool exact = true);
		IList<Artist> RangeArtists(char start, char end);
		IList<Artist> LimitArtists(int index, int duration = Int32.MinValue);
		IList<Album> AlbumsForArtistId(int artistId);
		IList<Artist> AllWithNoMusicBrainzId();
	}
}

