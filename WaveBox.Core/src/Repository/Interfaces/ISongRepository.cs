using System;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.Model.Repository {
    public interface ISongRepository {
        Song SongForId(int songId);
        IList<Song> SongsForIds(IList<int> songIds);
        IList<Song> AllSongs();
        bool InsertSong(Song song, bool replace = false);
        int CountSongs();
        long TotalSongSize();
        long TotalSongDuration();
        IList<Song> SearchSongs(string field, string query, bool exact = true);
        IList<Song> RangeSongs(char start, char end);
        IList<Song> LimitSongs(int index, int duration = Int32.MinValue);
    }
}
