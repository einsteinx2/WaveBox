using System;
using System.Collections.Generic;

namespace WaveBox.Model.Repository
{
	public interface IGenreRepository
	{
		List<Genre> AllGenres();
		List<Artist> ListOfArtists(int genreId);
		List<Album> ListOfAlbums(int genreId);
		List<Song> ListOfSongs(int genreId);
		List<Folder> ListOfFolders(int genreId);
	}
}

