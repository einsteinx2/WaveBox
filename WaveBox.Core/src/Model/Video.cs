using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Model;
using WaveBox.Static;
using TagLib;
using Newtonsoft.Json;
using System.Diagnostics;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Model
{
	public class Video : MediaItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static readonly string[] ValidExtensions = { ".m4v", ".mp4", ".mpg", ".mkv", ".avi" };

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public override ItemType ItemType { get { return ItemType.Video; } }

		[JsonProperty("itemTypeId"), IgnoreRead, IgnoreWrite]
		public override int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("width")]
		public int? Width { get; set; }
		
		[JsonProperty("height")]
		public int? Height { get; set; }

		[JsonProperty("aspectRatio")]
		public float? AspectRatio
		{ 
			get 
			{
				if ((object)Width == null || (object)Height == null || Height == 0)
				{
					return null;
				}

				return (float)Width / (float)Height;
			}
		}

		public Video()
		{
		}
		
		public static List<Video> AllVideos()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<Video>("SELECT * FROM Video");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return new List<Video>();
		}

		public static int CountVideos()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(ItemId) FROM Video");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return 0;
		}

		public static long TotalVideoSize()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.ExecuteScalar<long>("SELECT SUM(FileSize) FROM Video");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return 0;
		}

		public static long TotalVideoDuration()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.ExecuteScalar<long>("SELECT SUM(Duration) FROM Video");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return 0;
		}

		public static List<Video> SearchVideos(string field, string query, bool exact = true)
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
				conn = Database.GetSqliteConnection();
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
				conn.Close();
			}

			return new List<Video>();
		}

		public override void InsertMediaItem()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				conn.InsertLogged(this, InsertType.Replace);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			Art.UpdateArtItemRelationship(ArtId, ItemId, true);
			Art.UpdateArtItemRelationship(ArtId, FolderId, false); // Only update a folder art relationship if it has no folder art
		}

		public static int CompareVideosByFileName(Video x, Video y)
		{
			return x.FileName.CompareTo(y.FileName);
		}

		public class Factory
		{
			public Video CreateVideo(int videoId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
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
					conn.Close();
				}

				return new Video();
			}
		}
	}
}
