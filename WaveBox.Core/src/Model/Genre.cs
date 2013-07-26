using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model
{
	public class Genre
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public int? GenreId { get; set; }

		public string GenreName { get; set; }

		public Genre()
		{
		}

		public void InsertGenre()
		{
			if (GenreName == null)
			{
				// Can't insert a genre with no name
				return;
			}

			int? itemId = Injection.Kernel.Get<IItemRepository>().GenerateItemId(ItemType.Genre);
			if (itemId == null)
			{
				return;
			}
			
			ISQLiteConnection conn = null;
			try
			{
				// insert the genre into the database
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				GenreId = itemId;
				int affected = conn.InsertLogged(this);

				if (affected == 0)
				{
					GenreId = null;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
		}

		public List<Artist> ListOfArtists()
		{
			if (GenreId == null)
				return new List<Artist>();

			return Injection.Kernel.Get<IGenreRepository>().ListOfArtists((int)GenreId);
		}

		public List<Album> ListOfAlbums()
		{
			if (GenreId == null)
				return new List<Album>();

			return Injection.Kernel.Get<IGenreRepository>().ListOfAlbums((int)GenreId);
		}

		public List<Song> ListOfSongs()
		{
			if (GenreId == null)
				return new List<Song>();

			return Injection.Kernel.Get<IGenreRepository>().ListOfSongs((int)GenreId);
		}

		public List<Folder> ListOfFolders()
		{
			if (GenreId == null)
				return new List<Folder>();

			return Injection.Kernel.Get<IGenreRepository>().ListOfFolders((int)GenreId);
		}

		public static int CompareGenresByName(Genre x, Genre y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.GenreName, y.GenreName);
		}
	}
}
