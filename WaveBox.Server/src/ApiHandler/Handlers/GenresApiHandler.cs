using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.ApiResponse;
using WaveBox.Core;

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
			IList<Genre> listOfGenres = new List<Genre>();
			IList<Folder> listOfFolders = new List<Folder>();
			IList<Artist> listOfArtists = new List<Artist>();
			IList<Album> listOfAlbums = new List<Album>();
			IList<Song> listOfSongs = new List<Song>();

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

				Genre genre = Injection.Kernel.Get<IGenreRepository>().GenreForId(id);

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
				listOfGenres = Injection.Kernel.Get<IGenreRepository>().AllGenres();
			}

			// Return all results
			try
			{
				string json = JsonConvert.SerializeObject(new GenresResponse(null, listOfGenres, listOfFolders, listOfArtists, listOfAlbums, listOfSongs), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
