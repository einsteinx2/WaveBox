using System;
using WaveBox.Core.Injection;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Static;
using Ninject;

namespace WaveBox.Model.Repository
{
	public class GenreRepository : IGenreRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public GenreRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public List<Genre> AllGenres()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Genre>("SELECT * FROM Genre ORDER BY GenreName");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return new List<Genre>();
		}

		public List<Artist> ListOfArtists(int genreId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<Artist>("SELECT Artist.* " +
				                          "FROM Genre " + 
				                          "LEFT JOIN Song ON Song.GenreId = Genre.GenreId " +
				                          "LEFT JOIN Artist ON Song.ArtistId = Artist.ArtistId " +
				                          "WHERE Genre.GenreId = ? GROUP BY Artist.ArtistId", genreId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return new List<Artist>();
		}

		public List<Album> ListOfAlbums(int genreId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<Album>("SELECT Album.* " +
				                         "FROM Genre " + 
				                         "LEFT JOIN Song ON Song.GenreId = Genre.GenreId " +
				                         "LEFT JOIN Album ON Song.AlbumId = Album.ItemId " +
				                         "WHERE Genre.GenreId = ? GROUP BY Album.ItemId", genreId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return new List<Album>();
		}

		public List<Song> ListOfSongs(int genreId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<Song>("SELECT Song.*, Genre.GenreName " +
				                        "FROM Genre " + 
				                        "LEFT JOIN Song ON Song.GenreId = Genre.GenreId " +
				                        "WHERE Genre.GenreId = ? GROUP BY Song.ItemId", genreId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return new List<Song>();
		}

		public List<Folder> ListOfFolders(int genreId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<Folder>("SELECT Folder.* " +
				                          "FROM Genre " + 
				                          "LEFT JOIN Song ON Song.GenreId = Genre.GenreId " +
				                          "LEFT JOIN Folder ON Song.FolderId = Folder.FolderId " +
				                          "WHERE Genre.GenreId = ? GROUP BY Folder.FolderId", genreId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return new List<Folder>();
		}

	}
}

