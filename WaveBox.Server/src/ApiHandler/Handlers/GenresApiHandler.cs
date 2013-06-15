using System;
using WaveBox.Model;
using System.Collections.Generic;
using Newtonsoft.Json;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	public class GenresApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for FoldersApiHandler
		/// </summary>
		public GenresApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process returns a JSON response list of genres
		/// </summary>
		public void Process()
		{
			// Generate return lists of folders, songs, videos
			List<Genre> listOfGenres = new List<Genre>();
			List<Folder> listOfFolders = new List<Folder>();
			List<Artist> listOfArtists = new List<Artist>();
			List<Album> listOfAlbums = new List<Album>();
			List<Song> listOfSongs = new List<Song>();
			//List<Video> listOfVideos = new List<Video>();

			// Try to get the folder id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}
			
			if (success)
			{
				string type = "artists"; // Default to artist
				if (Uri.Parameters.ContainsKey("type"))
				{
					type = Uri.Parameters["type"];
				}

				Genre genre = new Genre.Factory().CreateGenre(id);

				switch (type)
				{
					case "folders":
						listOfFolders = genre.ListOfFolders();
						break;
					case "albums":
						listOfAlbums = genre.ListOfAlbums();
						break;
					case "songs":
						listOfSongs = genre.ListOfSongs();
						break;
					case "artists":
					default:
						listOfArtists = genre.ListOfArtists();
						break;
				}
			}
			else
			{
				// No id parameter
				listOfGenres = Genre.AllGenres();
			}
			
			// Return all results
			try
			{
				string json = JsonConvert.SerializeObject(new GenresResponse(null, listOfGenres, listOfFolders, listOfArtists, listOfAlbums, listOfSongs), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
		
		private class GenresResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("genres")]
			public List<Genre> Genres { get; set; }
			
			[JsonProperty("folders")]
			public List<Folder> Folders { get; set; }
			
			[JsonProperty("artists")]
			public List<Artist> Artists { get; set; }

			[JsonProperty("albums")]
			public List<Album> Albums { get; set; }
			
			[JsonProperty("songs")]
			public List<Song> Songs { get; set; }
			
			//[JsonProperty("videos")]
			//public List<Video> Videos { get; set; }
			
			public GenresResponse(string error, List<Genre> genres, List<Folder> folders, List<Artist> artists, List<Album> albums, List<Song> songs)
			{
				Error = error;
				Genres = genres;
				Folders = folders;
				Artists = artists;
				Albums = albums;
				Songs = songs;
			}
		}
	}
}
