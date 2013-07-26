using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IGenreRepository
	{
		Genre GenreForId(int? genreId);
		Genre GenreForName(string genreName);
		List<Genre> AllGenres();
		List<Artist> ListOfArtists(int genreId);
		List<Album> ListOfAlbums(int genreId);
		List<Song> ListOfSongs(int genreId);
		List<Folder> ListOfFolders(int genreId);
	}
}

