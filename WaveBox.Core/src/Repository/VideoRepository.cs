using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Linq;

namespace WaveBox.Core.Model.Repository
{
	public class VideoRepository : IVideoRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public VideoRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public Video VideoForId(int videoId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<Video>("SELECT * FROM Video WHERE ItemId = ?", videoId);

				foreach (Video v in result)
				{
					return v;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return new Video();
		}

		public List<Video> AllVideos()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Video>("SELECT * FROM Video");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return new List<Video>();
		}

		public int CountVideos()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(ItemId) FROM Video");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return 0;
		}

		public long TotalVideoSize()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				// Check if at least 1 video exists, to prevent exception if summing null
				int exists = conn.ExecuteScalar<int>("SELECT * FROM Video LIMIT 1");
				if (exists > 0)
				{
					return conn.ExecuteScalar<long>("SELECT SUM(FileSize) FROM Video");
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return 0;
		}

		public long TotalVideoDuration()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				// Check if at least 1 video exists, to prevent exception if summing null
				int exists = conn.ExecuteScalar<int>("SELECT * FROM Video LIMIT 1");
				if (exists > 0)
				{
					return conn.ExecuteScalar<long>("SELECT SUM(Duration) FROM Video");
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return 0;
		}

		public List<Video> SearchVideos(string field, string query, bool exact = true)
		{
			if (query == null)
			{
				return new List<Video>();
			}

			// Set default field, if none provided
			if (field == null)
			{
				field = "FileName";
			}

			// Check to ensure a valid query field was set
			if (!new string[] {"ItemId", "FolderId", "Duration", "Bitrate", "FileSize",
				"LastModified", "FileName", "Width", "Height", "FileType",
				"GenereId"}.Contains(field))
			{
				return new List<Video>();
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				if (exact)
				{
					// Search for exact match
					return conn.Query<Video>("SELECT * FROM Video WHERE " + field + " = ? ORDER BY FileName", query);
				}
				else
				{
					// Search for fuzzy match (containing query)
					return conn.Query<Video>("SELECT * FROM Video WHERE " + field + " LIKE ? ORDER BY FileName", "%" + query + "%");
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return new List<Video>();
		}

		// Return a list of videos titled between a range of (a-z, A-Z, 0-9 characters)
		public List<Video> RangeVideos(char start, char end)
		{
			// Ensure characters are alphanumeric, return empty list if either is not
			if (!Char.IsLetterOrDigit(start) || !Char.IsLetterOrDigit(end))
			{
				return new List<Video>();
			}

			string s = start.ToString();
			// Add 1 to character to make end inclusive
			string en = Convert.ToChar((int)end + 1).ToString();

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				List<Video> videos;
				videos = conn.Query<Video>("SELECT * FROM Video " +
				                           "WHERE Video.FileName BETWEEN LOWER(?) AND LOWER(?) " +
				                           "OR Video.FileName BETWEEN UPPER(?) AND UPPER(?)", s, en, s, en);

				videos.Sort(Video.CompareVideosByFileName);
				return videos;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			// We had an exception somehow, so return an empty list
			return new List<Video>();
		}

		// Return a list of videos using SQL LIMIT x,y where X is starting index and Y is duration
		public List<Video> LimitVideos(int index, int duration = Int32.MinValue)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();

				// Begin building query
				List<Video> videos;

				string query = "SELECT * FROM Video LIMIT ? ";

				// Add duration to LIMIT if needed
				if (duration != Int32.MinValue && duration > 0)
				{
					query += ", ?";
				}

				// Run query, sort, send it back
				videos = conn.Query<Video>(query, index, duration);
				videos.Sort(Video.CompareVideosByFileName);
				return videos;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			// We had an exception somehow, so return an empty list
			return new List<Video>();
		}
	}
}

