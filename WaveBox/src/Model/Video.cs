using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox.Model;
using WaveBox.Singletons;
using System.IO;
using TagLib;
using Newtonsoft.Json;
using System.Diagnostics;

namespace WaveBox.Model
{
	public class Video : MediaItem
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static readonly string[] ValidExtensions = { ".m4v", ".mp4", ".mpg", ".mkv", ".avi" };

		[JsonIgnore]
		public override ItemType ItemType { get { return ItemType.Video; } }

		[JsonProperty("itemTypeId")]
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
					return null;

				return (float)Width / (float)Height;
			}
		}

		public Video()
		{
		}

		public Video(IDataReader reader)
		{
			SetPropertiesFromQueryReader(reader);
		}
		
		public Video(int videoId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM video " +
				                                     "WHERE video_id = @videoid", conn);
				q.AddNamedParam("@videoid", videoId);
				
				q.Prepare();
				reader = q.ExecuteReader();
				
				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public Video(string filePath, int? folderId, TagLib.File file)
		{
			// We need to check to make sure the tag isn't corrupt before handing off to this method, anyway, so just feed in the tag
			// file that we checked for corruption.
			//TagLib.File file = TagLib.File.Create(fsFile.FullName);
			
			ItemId = Item.GenerateItemId(ItemType.Video);
			if (ItemId == null)
			{
				return;
			}
			
			FileInfo fsFile = new FileInfo(filePath);
			//TagLib.Tag tag = file.Tag;
			//var lol = file.Properties.Codecs;
			FolderId = folderId;
			
			FileType = FileType.FileTypeForTagLibMimeType(file.MimeType);

			if (FileType == FileType.Unknown)
			{
				if (logger.IsInfoEnabled) logger.Info("\"" + filePath + "\" Unknown file type: " + file.Properties.Description);
			}

			Width = file.Properties.VideoWidth;
			Height = file.Properties.VideoHeight;
			Duration = Convert.ToInt32(file.Properties.Duration.TotalSeconds);
			Bitrate = file.Properties.AudioBitrate;
			FileSize = fsFile.Length;
			LastModified = Convert.ToInt64(fsFile.LastWriteTime.Ticks);
			FileName = fsFile.Name;

			// Generate an art id from the embedded art, if it exists
			int? artId = new Art(file).ArtId;

			// If there was no embedded art, use the folder's art
			artId = (object)artId == null ? Art.ArtIdForItemId(FolderId) : artId;

			// Create the art/item relationship
			Art.UpdateArtItemRelationship(artId, ItemId, true);
		}

		private void SetPropertiesFromQueryReader(IDataReader reader)
		{
			try
			{
				ItemId = reader.GetInt32(reader.GetOrdinal("video_id"));
				FolderId = reader.GetInt32OrNull(reader.GetOrdinal("video_folder_id"));
				Duration = reader.GetInt32OrNull(reader.GetOrdinal("video_duration"));
				Bitrate = reader.GetInt32OrNull(reader.GetOrdinal("video_bitrate"));
				FileSize = reader.GetInt64OrNull(reader.GetOrdinal("video_file_size"));
				LastModified = reader.GetInt64OrNull(reader.GetOrdinal("video_last_modified"));
				FileName = reader.GetStringOrNull(reader.GetOrdinal("video_file_name"));
				Width = reader.GetInt32OrNull(reader.GetOrdinal("video_width"));
				Height = reader.GetInt32OrNull(reader.GetOrdinal("video_height"));
				int? fileTypeId = reader.GetInt32OrNull(reader.GetOrdinal("video_file_type_id"));
				if (fileTypeId != null)
				{
					FileType = FileType.FileTypeForId((int)fileTypeId);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		public static List<Video> AllVideos()
		{
			List<Video> allVideos = new List<Video>();
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM video", conn);
				
				q.Prepare();
				reader = q.ExecuteReader();
				
				//Stopwatch sw = new Stopwatch();
				while (reader.Read())
				{
					//sw.Start();
					allVideos.Add(new Video(reader));
					//if (logger.IsInfoEnabled) logger.Info("Elapsed: {0}ms", sw.ElapsedMilliseconds);
					//sw.Restart();
				}
				//sw.Stop();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return allVideos;
		}

		public static int? CountVideos()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			int? count = 0;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT count(video_id) FROM video", conn);
				object result = q.ExecuteScalar();
				if (result != DBNull.Value)
				{
					count = Convert.ToInt32(result);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return count;
		}

		public static long? TotalVideoSize()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			long? total = 0;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT sum(video_file_size) FROM video", conn);
				object result = q.ExecuteScalar();
				if (result != DBNull.Value)
				{
					total = Convert.ToInt64(result);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return total;
		}

		public static List<Video> SearchVideo(string query)
		{
			List<Video> result = new List<Video>();

			if (query == null)
			{
				return result;
			}

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM video WHERE video_file_name LIKE @videofilename", conn);
				q.AddNamedParam("@videofilename", "%" + query + "%");
				q.Prepare();
				reader = q.ExecuteReader();

				Video v;

				while(reader.Read())
				{
					v = new Video(reader);
					result.Add(v);
				}
			}
			catch(Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return result;
		}

		public static bool VideoNeedsUpdating(string filePath, int? folderId, out bool isNew, out int? songId)
		{
			string fileName = Path.GetFileName(filePath);
			long lastModified = Convert.ToInt64(System.IO.File.GetLastWriteTime(filePath).Ticks);
			bool needsUpdating = true;
			isNew = true;
			songId = null;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT video_id, video_last_modified, video_file_size " +
				                                     "FROM video WHERE video_folder_id = @folderid AND video_file_name = @filename", conn);
				q.AddNamedParam("@folderid", folderId);
				q.AddNamedParam("@filename", fileName);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					isNew = false;

					songId = reader.GetInt32(0);
					long lastModDb = reader.GetInt64(1);
					if (lastModDb == lastModified)
					{
						needsUpdating = false;
					}
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return needsUpdating;
		}

		public override void InsertMediaItem()
		{
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				// insert the song into the database
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("REPLACE INTO video (video_id, video_folder_id, video_duration, video_bitrate, video_file_size, video_last_modified, video_file_name, video_width, video_height, video_file_type_id) " + 
				                                     "VALUES (@videoid, @folderid, @duration, @bitrate, @filesize, @lastmod, @filename, @width, @height, @filetype)"
				                                     , conn);

				q.AddNamedParam("@videoid", ItemId);
				q.AddNamedParam("@folderid", FolderId);
				q.AddNamedParam("@duration", Duration);
				q.AddNamedParam("@bitrate", Bitrate);
				q.AddNamedParam("@filesize", FileSize);
				q.AddNamedParam("@lastmod", LastModified);
				q.AddNamedParam("@filename", FileName);
				q.AddNamedParam("@width", Width);
				q.AddNamedParam("@height", Height);
				q.AddNamedParam("@filetype", (int)FileType);

				q.Prepare();
				
				q.ExecuteNonQueryLogged();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			Art.UpdateArtItemRelationship(ArtId, ItemId, true);
			Art.UpdateArtItemRelationship(ArtId, FolderId, false); // Only update a folder art relationship if it has no folder art
		}

		public static int CompareVideosByFileName(Video x, Video y)
		{
			return x.FileName.CompareTo(y.FileName);
		}
	}
}
