using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using WaveBox.Core.Injection;
using WaveBox.Static;
using WaveBox.Model.Repository;

namespace WaveBox.Model
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

		public class Factory
		{
			public Genre CreateGenre(int? genreId)
			{
				if ((object)genreId == null)
				{
					return new Genre();
				}

				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Genre>("SELECT * FROM Genre WHERE GenreId = ?", genreId);

					foreach (Genre g in result)
					{
						return g;
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

				Genre genre = new Genre();
				genre.GenreId = genreId;
				return genre;
			}

			private static object genreMemCacheLock = new object();
			private static List<Genre> memCachedGenres = new List<Genre>();
			public Genre CreateGenre(string genreName)
			{
				if ((object)genreName == null)
				{
					return new Genre();
				}

				lock (genreMemCacheLock)
				{
					// First check to see if the genre is in mem cache
					Genre genre = (from g in memCachedGenres where g.GenreName.Equals(genreName) select g).FirstOrDefault();

					if ((object)genre != null)
					{
						// We got a match, so use the genre id
						return genre;
					}
					else
					{
						// Retreive the genre id if it exists
						ISQLiteConnection conn = null;
						try
						{
							conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
							var result = conn.DeferredQuery<Genre>("SELECT * FROM Genre WHERE GenreName = ?", genreName);

							foreach (Genre g in result)
							{
								return g;
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

						// If this genre didn't exist, generate an id and insert it
						Genre genre2 = new Genre();
						genre2.GenreName = genreName;
						genre2.InsertGenre();
						return genre2;
					}
				}
			}
		}
	}
}
