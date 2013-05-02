using System;
using System.Data;
using WaveBox.DataModel.Singletons;
using System.Collections.Generic;
using NLog;
using System.Linq;

namespace WaveBox.DataModel.Model
{
	public class Genre
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public int? GenreId { get; set; }

		public string GenreName { get; set; }

		public Genre(int? genreId)
		{
			if ((object)genreId == null)
				return;

			GenreId = genreId;

			// Retreive the genre id if it exists
			IDbConnection conn = null;
			IDataReader reader = null;
			
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT genre_name FROM genre WHERE genre_id = @genreid", conn);
				q.AddNamedParam("@genreid", genreId);
				
				q.Prepare();
				reader = q.ExecuteReader();
				
				if (reader.Read())
				{
					GenreName = reader.GetString(0);
				}
			}
			catch (Exception e)
			{
				logger.Error("[COVERART(1)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}
		
		private static object genreMemCacheLock = new object();
		private static List<Genre> memCachedGenres = new List<Genre>();
		public Genre(string genreName)
		{
			if ((object)genreName == null)
				return;

			lock(genreMemCacheLock)
			{
				GenreName = genreName;

				// First check to see if the genre is in mem cache
				Genre genre = (from g in memCachedGenres
				               where g.GenreName.Equals(genreName)
				               select g).FirstOrDefault();
				
				if ((object)genre != null)
				{
					// We got a match, so use the genre id
					GenreId = genre.GenreId;
				}
				else
				{
					// Retreive the genre id if it exists
					IDbConnection conn = null;
					IDataReader reader = null;
					
					try
					{
						conn = Database.GetDbConnection();
						IDbCommand q = Database.GetDbCommand("SELECT genre_id FROM genre WHERE genre_name = @genrename", conn);
						q.AddNamedParam("@genrename", genreName);
						
						q.Prepare();
						reader = q.ExecuteReader();
						
						if (reader.Read())
						{
							GenreId = reader.GetInt32OrNull(0);
						}
					}
					catch (Exception e)
					{
						logger.Error("[COVERART(1)] ERROR: " + e);
					}
					finally
					{
						Database.Close(conn, reader);
					}
					
					// If this genre didn't exist, generate an id and insert it
					if ((object)GenreId == null)
					{
						InsertGenre();
					}
				}
			}
		}

		public Genre(IDataReader reader)
		{
			SetPropertiesFromQueryReader(reader);
		}

		private void SetPropertiesFromQueryReader(IDataReader reader)
		{
			if ((object)reader == null)
				return;

			GenreId = reader.GetInt32OrNull(reader.GetOrdinal("genre_id"));
			GenreName = reader.GetStringOrNull(reader.GetOrdinal("genre_name"));
		}

		public void InsertGenre()
		{
			if (GenreName == null)
			{
				// Can't insert a genre with no name
				return;
			}

			int? itemId = Item.GenerateItemId(ItemType.Genre);
			if (itemId == null)
			{
				return;
			}
			
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				// insert the genre into the database
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO genre (genre_id, genre_name)" + 
				                                     "VALUES (@genreid, @genrename)"
				                                     , conn);
				
				q.AddNamedParam("@genreid", itemId);
				q.AddNamedParam("@genrename", this.GenreName);
				q.Prepare();

				if (q.ExecuteNonQueryLogged() > 0)
				{
					GenreId = itemId;
				}
				
				return;
			}
			catch (Exception e)
			{
				logger.Error("[GENRE(1)] " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public List<Artist> ListOfArtists()
		{
			List<Artist> listOfArtists = new List<Artist>();

			// Retreive the genre id if it exists
			IDbConnection conn = null;
			IDataReader reader = null;
			
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT artist.* " +
				                                     "FROM genre " + 
				                                     "LEFT JOIN song ON song.song_genre_id = genre.genre_id " +
				                                     "LEFT JOIN artist ON song.song_artist_id = artist.artist_id " +
				                                     "WHERE genre_id = @genreid GROUP BY artist.artist_id", conn);

				q.AddNamedParam("@genreid", GenreId);
				
				q.Prepare();
				reader = q.ExecuteReader();
				
				while (reader.Read())
				{
					listOfArtists.Add(new Artist(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error("[GENRE(2)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return listOfArtists;
		}

		public List<Album> ListOfAlbums()
		{
			List<Album> listOfAlbums = new List<Album>();
			
			// Retreive the genre id if it exists
			IDbConnection conn = null;
			IDataReader reader = null;
			
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT album.* " +
				                                     "FROM genre " + 
				                                     "LEFT JOIN song ON song.song_genre_id = genre.genre_id " +
				                                     "LEFT JOIN album ON song.song_album_id = album.album_id " +
				                                     "WHERE genre_id = @genreid GROUP BY album.album_id", conn);
				
				q.AddNamedParam("@genreid", GenreId);
				
				q.Prepare();
				reader = q.ExecuteReader();
				
				while (reader.Read())
				{
					listOfAlbums.Add(new Album(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error("[GENRE(3)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
			
			return listOfAlbums;
		}

		public List<Song> ListOfSongs()
		{
			List<Song> listOfSongs = new List<Song>();
			
			// Retreive the genre id if it exists
			IDbConnection conn = null;
			IDataReader reader = null;
			
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, genre.genre_name " +
				                                     "FROM genre " + 
				                                     "LEFT JOIN song ON song.song_genre_id = genre.genre_id " +
				                                     "WHERE genre_id = @genreid GROUP BY song.song_id", conn);
				
				q.AddNamedParam("@genreid", GenreId);
				
				q.Prepare();
				reader = q.ExecuteReader();
				
				while (reader.Read())
				{
					listOfSongs.Add(new Song(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error("[GENRE(4)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
			
			return listOfSongs;
		}

		public List<Folder> ListOfFolders()
		{
			List<Folder> listOfFolders = new List<Folder>();
			
			// Retreive the genre id if it exists
			IDbConnection conn = null;
			IDataReader reader = null;
			
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT folder.* " +
				                                     "FROM genre " + 
				                                     "LEFT JOIN song ON song.song_genre_id = genre.genre_id " +
				                                     "LEFT JOIN folder ON song.song_folder_id = folder.folder_id " +
				                                     "WHERE genre_id = @genreid GROUP BY folder.folder_id", conn);
				
				q.AddNamedParam("@genreid", GenreId);
				
				q.Prepare();
				reader = q.ExecuteReader();
				
				while (reader.Read())
				{
					listOfFolders.Add(new Folder(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error("[GENRE(5)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
			
			return listOfFolders;
		}

		public static List<Genre> AllGenres()
		{
			List<Genre> genres = new List<Genre>();
			
			IDbConnection conn = null;
			IDataReader reader = null;
			
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM genre", conn);
				q.Prepare();
				reader = q.ExecuteReader();
				
				while (reader.Read())
				{
					genres.Add(new Genre(reader));
				}
			}
			catch (Exception e)
			{
				logger.Error("[GENRE(7)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
			
			genres.Sort(Genre.CompareGenresByName);
			
			return genres;
		}

		public static int CompareGenresByName(Genre x, Genre y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.GenreName, y.GenreName);
		}
	}
}

