using System;
using System.Collections.Generic;

namespace WaveBox.Model.Repository
{
	public interface IArtistRepository
	{
		bool InsertArtist(string artistName);
		Artist ArtistForName(string artistName);
		List<Artist> AllArtists();
		int CountArtists();
		List<Artist> SearchArtists(string field, string query, bool exact = true);
		List<Artist> RangeArtists(char start, char end);
		List<Artist> LimitArtists(int index, int duration = Int32.MinValue);
	}
}

