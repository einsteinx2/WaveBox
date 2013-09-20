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
using WaveBox.Core.Static;
using System.Linq;

namespace WaveBox.ApiHandler.Handlers
{
	public class GenresApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "genres"; } }

		// API handler is read-only, so no permissions checks needed
		public bool CheckPermission(User user, string action)
		{
			return true;
		}

		/// <summary>
		/// Process returns a JSON response list of genres
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Generate return lists of folders, songs, videos
			IList<Genre> listOfGenres = new List<Genre>();
			IList<Folder> listOfFolders = new List<Folder>();
			IList<Artist> listOfArtists = new List<Artist>();
			IList<Album> listOfAlbums = new List<Album>();
			IList<Song> listOfSongs = new List<Song>();
			PairList<string, int> sectionPositions = new PairList<string, int>();

			// ID specified
			if (uri.Id != null)
			{
				// Default: artists
				string type = "artists";
				if (uri.Parameters.ContainsKey("type"))
				{
					type = uri.Parameters["type"];
				}

				// Get single genre, add it for output
				Genre genre = Injection.Kernel.Get<IGenreRepository>().GenreForId((int)uri.Id);
				listOfGenres.Add(genre);

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
				sectionPositions = Utility.SectionPositionsFromSortedList(new List<IGroupingItem>(listOfGenres.Select(c => (IGroupingItem)c)));
			}

			// Return all results
			processor.WriteJson(new GenresResponse(null, listOfGenres, listOfFolders, listOfArtists, listOfAlbums, listOfSongs, sectionPositions));
			return;
		}
	}
}
