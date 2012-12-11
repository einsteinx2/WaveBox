using System;
using System.Collections.Generic;
using System.Web;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	public class SearchApiHandler : IApiHandler
	{		
		//private static Logger logger = LogManager.GetCurrentClassLogger();

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		public SearchApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		public void Process()
		{
			List<Artist> artists = new List<Artist>();
			List<Album> albums = new List<Album>();
			List<Song> songs = new List<Song>();

			if(Uri.Parameters.ContainsKey("q"))
			{
				string query = HttpUtility.UrlDecode(Uri.Parameters["q"]);
				if(query.Length > 0)
				{	 
					if(Uri.Parameters.ContainsKey("t"))
					{
						foreach(string type in Uri.Parameters["t"].Split(','))
						{
							switch(type)
							{
								case "artist":
								case "artists":
									artists = Artist.SearchArtist(query);
									break;
								case "album":
								case "albums":
									albums = Album.SearchAlbum(query);
									break;
								case "song":
								case "songs":
									songs = Song.SearchSong(query);
									break;
								default:
									artists = Artist.SearchArtist(query);
									albums = Album.SearchAlbum(query);
									songs = Song.SearchSong(query);
									break;
							}
						}
					}
					else
					{
						artists = Artist.SearchArtist(query);
						albums = Album.SearchAlbum(query);
						songs = Song.SearchSong(query);
					}

					string error = null;
					if(artists.Count == 0 && albums.Count == 0 && songs.Count == 0)
					{
						error = "No search results found for query '" + query + "'";
					}
			
					string json = JsonConvert.SerializeObject(new SearchResponse(error, artists, albums, songs), Settings.JsonFormatting);
					Processor.WriteJson(json);
				}
				else
				{
					string json = JsonConvert.SerializeObject(new SearchResponse("Query cannot be empty", artists, albums, songs), Settings.JsonFormatting);
					Processor.WriteJson(json);
				}
			}
			else
			{
				string json = JsonConvert.SerializeObject(new SearchResponse("No search query provided", artists, albums, songs), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
		}

		private class SearchResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("artists")]
			public List<Artist> Artists { get; set; }

			[JsonProperty("albums")]
			public List<Album> Albums { get; set; }

			[JsonProperty("songs")]
			public List<Song> Songs { get; set; }

			public SearchResponse(string error, List<Artist> artists, List<Album> albums, List<Song> songs)
			{
				Error = error;
				Artists = artists;
				Albums = albums;
				Songs = songs;
			}
		}
	}
}

