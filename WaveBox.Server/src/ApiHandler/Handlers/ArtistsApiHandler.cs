using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Static;
using WaveBox.Model;
using Newtonsoft.Json;
using WaveBox.TcpServer.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class ArtistsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for ArtistsApiHandler class
		/// </summary>
		public ArtistsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process returns an ArtistsResponse containing a list of artists, albums, and songs
		/// </summary>
		public void Process()
		{
			// Return lists to be passed as JSON
			List<Artist> listOfArtists = new List<Artist>();
			List<Song> listOfSongs = new List<Song>();
			List<Album> listOfAlbums = new List<Album>();

			// Last.fm integration
			// Info regarding artist
			string lastfmInfo = null;
			// Enable/disable Last.fm info
			bool includeLfm = false;

			// Try to get the artist id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			// Optionally use Last.fm to gather information
			if (Uri.Parameters.ContainsKey("lastfmInfo"))
			{
				bool.TryParse(Uri.Parameters["lastfmInfo"], out includeLfm);
			}

			// On valid key, return a specific artist, and a list of their albums
			if (success)
			{
				Artist artist = new Artist.Factory().CreateArtist(id);
				listOfArtists.Add(artist);
				listOfAlbums = artist.ListOfAlbums();

				// Grab Last.fm data if requested
				if (includeLfm == true)
				{
					lastfmInfo = Lastfm.GetArtistInfo(artist);
				}

				// If requested, include list of songs in response as well
				string includeSongs;
				Uri.Parameters.TryGetValue("includeSongs", out includeSongs);
				if ((object)includeSongs != null && includeSongs.ToLower() == "true")
				{
					listOfSongs = artist.ListOfSongs();
				}
			}
			else
			{
				// On invalid key, return all artists
				listOfArtists = Artist.AllArtists();
			}

			try
			{
				// Write JSON to HTTP response
				string json = JsonConvert.SerializeObject(new ArtistsResponse(null, listOfArtists, listOfAlbums, listOfSongs, lastfmInfo), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		private class ArtistsResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("artists")]
			public List<Artist> Artists { get; set; }

			[JsonProperty("albums")]
			public List<Album> Albums { get; set; }

			[JsonProperty("songs")]
			public List<Song> Songs { get; set; }

			[JsonProperty("lastfmInfo")]
			public dynamic LastfmInfo { get; set; }

			public ArtistsResponse(string error, List<Artist> artists, List<Album> albums, List<Song> songs)
			{
				Error = error;
				Artists = artists;
				Songs = songs;
				Albums = albums;
				LastfmInfo = null;
			}

			public ArtistsResponse(string error, List<Artist> artists, List<Album> albums, List<Song> songs, string lastfmInfo)
			{
				Error = error;
				Artists = artists;
				Songs = songs;
				Albums = albums;

				// Deserialize Last.fm info if requested
				if (lastfmInfo != null)
				{
					var jsonParse = JsonConvert.DeserializeObject(lastfmInfo);
					LastfmInfo = jsonParse;
				}
				else
				{
					LastfmInfo = null;
				}
			}
		}
	}
}
