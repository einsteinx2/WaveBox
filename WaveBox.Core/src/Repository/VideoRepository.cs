using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Linq;
using WaveBox.Core.Extensions;

namespace WaveBox.Core.Model.Repository
{
	public class VideoRepository : IVideoRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public VideoRepository(IDatabase database)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}

			this.database = database;
		}

		public Video VideoForId(int videoId)
		{
			return this.database.GetSingle<Video>("SELECT * FROM Video WHERE ItemId = ?", videoId);
		}

		public IList<Video> AllVideos()
		{
			return this.database.GetList<Video>("SELECT * FROM Video");
		}

		public int CountVideos()
		{
			return this.database.GetScalar<int>("SELECT COUNT(ItemId) FROM Video");
		}

		public long TotalVideoSize()
		{
			// Check if at least 1 video exists, to prevent exception if summing null
			int exists = this.database.GetScalar<int>("SELECT * FROM Video LIMIT 1");
			if (exists > 0)
			{
				return this.database.GetScalar<long>("SELECT SUM(FileSize) FROM Video");
			}

			return 0;
		}

		public long TotalVideoDuration()
		{
			// Check if at least 1 video exists, to prevent exception if summing null
			int exists = this.database.GetScalar<int>("SELECT * FROM Video LIMIT 1");
			if (exists > 0)
			{
				return this.database.GetScalar<long>("SELECT SUM(Duration) FROM Video");
			}

			return 0;
		}

		public IList<Video> SearchVideos(string field, string query, bool exact = true)
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

			if (exact)
			{
				// Search for exact match
				return this.database.GetList<Video>("SELECT * FROM Video WHERE " + field + " = ? ORDER BY FileName COLLATE NOCASE", query);
			}

			// Search for fuzzy match (containing query)
			return this.database.GetList<Video>("SELECT * FROM Video WHERE " + field + " LIKE ? ORDER BY FileName COLLATE NOCASE", "%" + query + "%");
		}

		// Return a list of videos titled between a range of (a-z, A-Z, 0-9 characters)
		public IList<Video> RangeVideos(char start, char end)
		{
			// Ensure characters are alphanumeric, return empty list if either is not
			if (!Char.IsLetterOrDigit(start) || !Char.IsLetterOrDigit(end))
			{
				return new List<Video>();
			}

			string s = start.ToString();
			// Add 1 to character to make end inclusive
			string en = Convert.ToChar((int)end + 1).ToString();

			return this.database.GetList<Video>(
				"SELECT * FROM Video " +
				"WHERE Video.FileName BETWEEN LOWER(?) AND LOWER(?) " +
				"OR Video.FileName BETWEEN UPPER(?) AND UPPER(?) " +
				"ORDER BY Video.FileName COLLATE NOCASE",
			s, en, s, en);
		}

		// Return a list of videos using SQL LIMIT x,y where X is starting index and Y is duration
		public IList<Video> LimitVideos(int index, int duration = Int32.MinValue)
		{
			string query = "SELECT * FROM Video ORDER BY FileName LIMIT ? ";

			// Add duration to LIMIT if needed
			if (duration != Int32.MinValue && duration > 0)
			{
				query += ", ?";
			}

			return this.database.GetList<Video>(query, index, duration);
		}
	}
}
