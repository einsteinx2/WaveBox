using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using Bend.Util;

namespace WaveBox.ApiHandler.Handlers
{
	class ArtistsApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public ArtistsApiHandler(UriWrapper uriW, HttpProcessor sh, int userId)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			List<Artist> listOfArtists = new List<Artist>();
			List<Song> listOfSongs = new List<Song>();
			List<Album> listOfAlbums = new List<Album>();
			string json = "";

			if (_uriW.getUriPart(2) == null)
			{
				listOfArtists = new Artist().allArtists();
			}
			else
			{
				var artist = new Artist(Convert.ToInt32(_uriW.getUriPart(2)));
				listOfArtists.Add(artist);
				listOfSongs = artist.listOfSongs();
				listOfAlbums = artist.listOfAlbums();
			}

			json = JsonConvert.SerializeObject(new ArtistsResponse(null, listOfArtists, listOfAlbums, listOfSongs), Formatting.None);
			PmsHttpServer.sendJson(_sh, json);
		}
	}

	class ArtistsResponse
	{
		private string _error;
		public string error
		{
			get
			{
				return _error;
			}
			set
			{
				_error = value;
			}
		}

		private List<Artist> _artists;
		public List<Artist> artists
		{
			get
			{
				return _artists;
			}
			set
			{
				_artists = value;
			}
		}

		private List<Album> _albums;
		public List<Album> albums
		{
			get
			{
				return _albums;
			}
			set
			{
				_albums = value;
			}
		}

		private List<Song> _songs;
		public List<Song> songs
		{
			get
			{
				return _songs;
			}
			set
			{
				_songs = value;
			}
		}

		public ArtistsResponse(string Error, List<Artist> Artists, List<Album> Albums, List<Song> Songs)
		{
			error = Error;
			artists = Artists;
			songs = Songs;
			albums = Albums;
		}
	}
}
