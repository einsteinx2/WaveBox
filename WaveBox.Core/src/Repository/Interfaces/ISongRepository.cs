using System;
using System.Collections.Generic;

namespace WaveBox.Model.Repository
{
	public interface ISongRepository
	{
		IList<Song> SongsForIds(IList<int> songIds);
		IList<Song> AllSongs();
		int CountSongs();
		long TotalSongSize();
		long TotalSongDuration();
		List<Song> SearchSongs(string field, string query, bool exact = true);
		List<Song> RangeSongs(char start, char end);
		List<Song> LimitSongs(int index, int duration = Int32.MinValue);

	}
}

