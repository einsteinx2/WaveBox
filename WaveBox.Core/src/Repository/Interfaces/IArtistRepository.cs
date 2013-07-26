using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IArtistRepository
	{
		Artist ArtistForId(int? artistId);
		Artist ArtistForName(string artistName);
		bool InsertArtist(string artistName);
		Artist ArtistForNameOrCreate(string artistName);
		List<Artist> AllArtists();
		int CountArtists();
		List<Artist> SearchArtists(string field, string query, bool exact = true);
		List<Artist> RangeArtists(char start, char end);
		List<Artist> LimitArtists(int index, int duration = Int32.MinValue);
	}
}

