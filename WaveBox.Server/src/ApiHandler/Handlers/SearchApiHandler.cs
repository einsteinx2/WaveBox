using System;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	public class SearchApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		/// <summary>
		/// Constructor for SearchApiHandler
		/// </summary>
		public SearchApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		/// <summary>
		/// Process performs a search for a query with specified types
		/// </summary>
		public void Process()
		{
			// Lists to return as results
			IList<Artist> artists = new List<Artist>();
			IList<Album> albums = new List<Album>();
			IList<Song> songs = new List<Song>();
			IList<Video> videos = new List<Video>();

			// If a query is provided...
			if (Uri.Parameters.ContainsKey("query"))
			{
				// URL decode to strip any URL-encoded characters
				string query = HttpUtility.UrlDecode(Uri.Parameters["query"]);

				// Ensure query is not blank
				if (query.Length > 0)
				{
					// Check for query field
					string field = null;
					if (Uri.Parameters.ContainsKey("field"))
					{
						// Use input field for query
						field = HttpUtility.UrlDecode(Uri.Parameters["field"]);
					}

					// Check for exact match parameter
					bool exact = false;
					if (Uri.Parameters.ContainsKey("exact"))
					{
						if (Uri.Parameters["exact"].IsTrue())
						{
							exact = true;
						}
					}

					// If a query type is provided...
					if (Uri.Parameters.ContainsKey("type"))
					{
						// Iterate all comma-separated values in query type
						foreach (string type in Uri.Parameters["type"].Split(','))
						{
							// Return results, populating lists depending on parameters specified
							switch (type)
							{
								case "artists":
									artists = Injection.Kernel.Get<IArtistRepository>().SearchArtists(field, query, exact);
									break;
								case "albums":
									albums = Injection.Kernel.Get<IAlbumRepository>().SearchAlbums(field, query, exact);
									break;
								case "songs":
									songs = Injection.Kernel.Get<ISongRepository>().SearchSongs(field, query, exact);
									break;
								case "videos":
									videos = Injection.Kernel.Get<IVideoRepository>().SearchVideos(field, query, exact);
									break;
								default:
									artists = Injection.Kernel.Get<IArtistRepository>().SearchArtists(field, query, exact);
									albums = Injection.Kernel.Get<IAlbumRepository>().SearchAlbums(field, query, exact);
									songs = Injection.Kernel.Get<ISongRepository>().SearchSongs(field, query, exact);
									videos = Injection.Kernel.Get<IVideoRepository>().SearchVideos(field, query, exact);
									break;
							}
						}
					}
					else
					{
						// For no type, provide all types of data
						artists = Injection.Kernel.Get<IArtistRepository>().SearchArtists(field, query, exact);
						albums = Injection.Kernel.Get<IAlbumRepository>().SearchAlbums(field, query, exact);
						songs = Injection.Kernel.Get<ISongRepository>().SearchSongs(field, query, exact);
						videos = Injection.Kernel.Get<IVideoRepository>().SearchVideos(field, query, exact);
					}

					// On no results, return a 'harmless' error stating no results
					string error = null;
					if ((artists.Count == 0) && (albums.Count == 0) && (songs.Count == 0) && (videos.Count == 0))
					{
						error = "No search results found for query '" + query + "' on field '" + field + "'";
					}

					// Return all results
					string json = JsonConvert.SerializeObject(new SearchResponse(error, artists, albums, songs, videos), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
				}
				else
				{
					// Return error JSON for empty query
					string json = JsonConvert.SerializeObject(new SearchResponse("Query cannot be empty", artists, albums, songs, videos), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
				}
			}
			else
			{
				// Return error JSON for no query parameter
				string json = JsonConvert.SerializeObject(new SearchResponse("No search query provided", artists, albums, songs, videos), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				Processor.WriteJson(json);
			}
		}
	}
}
