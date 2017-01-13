using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository {
    public interface IGenreRepository {
        Genre GenreForId(int? genreId);
        Genre GenreForName(string genreName);
        IList<Genre> AllGenres();
        IList<Artist> ListOfArtists(int genreId);
        IList<Album> ListOfAlbums(int genreId);
        IList<Song> ListOfSongs(int genreId);
        IList<Folder> ListOfFolders(int genreId);
        bool InsertGenre(Genre genre, bool replace);
    }
}
