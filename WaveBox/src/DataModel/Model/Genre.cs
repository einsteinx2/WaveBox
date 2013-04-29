using System;
using System.Data;
using WaveBox.DataModel.Singletons;
using NLog;

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

		public Genre(string genreName)
		{
			if ((object)genreName == null)
				return;

			GenreName = genreName;

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

			// If this genre didn't exist, insert it
			if ((object)GenreId == null)
			{
				InsertGenre();
			}
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
	}
}

