using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Singletons;
using WaveBox.Model;
using System.Data;
using System.IO;
using System.Diagnostics;
using WaveBox.OperationQueue;

namespace WaveBox.FolderScanning
{
	class OrphanScanOperation : AbstractOperation
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override string OperationType { get { return "OrphanScanOperation"; } }

		long totalExistsTime = 0;

		public OrphanScanOperation(int secondsDelay) : base(secondsDelay)
		{
		}

		public override void Start()
		{
			Stopwatch sw = new Stopwatch();

			if (logger.IsInfoEnabled) logger.Info("---------------- ORPHAN SCAN ----------------");
			sw.Start();
			CheckFolders();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check folders: " + sw.ElapsedMilliseconds + "ms");

			sw.Restart();
			CheckSongs();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check songs: " + sw.ElapsedMilliseconds + "ms");

			sw.Restart();
			CheckArtists();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check artists: " + sw.ElapsedMilliseconds + "ms");

			sw.Restart();
			CheckAlbums();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check albums: " + sw.ElapsedMilliseconds + "ms");

			sw.Restart();
			CheckGenres();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check genres: " + sw.ElapsedMilliseconds + "ms");

			if (logger.IsInfoEnabled) logger.Info("check songs exists calls total time: " + (totalExistsTime / 10000) + "ms"); // Convert ticks to milliseconds, divide by 10,000
			if (logger.IsInfoEnabled) logger.Info("---------------------------------------------");
		}

		private void CheckFolders()
		{
			if (isRestart) 
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;
			ArrayList mediaFolderIds = new ArrayList();
			ArrayList orphanFolderIds = new ArrayList();

			foreach (Folder mediaFolder in Settings.MediaFolders) {
				mediaFolderIds.Add (mediaFolder.FolderId);
			}

			try 
			{
				conn = Database.GetDbConnection();

				IDbCommand q = Database.GetDbCommand("SELECT * FROM folder", conn);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read()) 
				{
					// storage for stuff we'll get.
					string path;
					int? folderId, mediaFolderId;

					// get ordinals
					int pathOrdinal = reader.GetOrdinal("folder_path");
					int folderIdOrdinal = reader.GetOrdinal("folder_id");
					int mediaFolderIdOrdinal = reader.GetOrdinal("folder_media_folder_id");

					if (reader.GetValue(pathOrdinal) != DBNull.Value) 
					{
						path = reader.GetString(reader.GetOrdinal("folder_path"));
					} 
					else
					{
						path = "";
					}

					if (reader.GetValue(folderIdOrdinal) != DBNull.Value)
					{
						folderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
					} 
					else 
					{
						folderId = null;
					}

					if (reader.GetValue(mediaFolderIdOrdinal) != DBNull.Value) 
					{
						mediaFolderId = reader.GetInt32(reader.GetOrdinal("folder_media_folder_id"));
					} 
					else
					{
						mediaFolderId = null;
					}

					if (mediaFolderId != null)
					{
						if (!mediaFolderIds.Contains(mediaFolderId) || !Directory.Exists(path)) 
						{
							logger.Info(folderId + " is orphaned");
							orphanFolderIds.Add(folderId);
						}
					}
				}

				foreach (int fid in orphanFolderIds) 
				{
					try 
					{
						IDbCommand q1 = Database.GetDbCommand("DELETE FROM folder WHERE folder_id = @folderid", conn);
						q1.AddNamedParam("@folderid", fid);

						q1.Prepare();
						q1.ExecuteNonQueryLogged();
					} 
					catch (Exception e) 
					{
						logger.Error("Failed to delete orphan " + fid + " : " + e);
					}

					try
					{
						IDbCommand q2 = Database.GetDbCommand("DELETE FROM song WHERE song_folder_id = @folderid", conn);
						q2.AddNamedParam("@folderid", fid);

						q2.Prepare();
						q2.ExecuteNonQueryLogged();
					} 
					catch (Exception e) 
					{
						logger.Error("Failed to delete songs for orphan " + fid + " : " + e);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed to delete orphan items : " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		private void CheckSongs()
		{
			if (isRestart)
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;
			ArrayList orphanSongIds = new ArrayList();
			int songid;
			string path, filename;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.song_id, song.song_file_name, folder.folder_path " +
					"FROM song " + 
					"LEFT JOIN folder ON song.song_folder_id = folder.folder_id", conn);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					songid = reader.GetInt32(reader.GetOrdinal("song_id"));
					filename = reader.GetString(reader.GetOrdinal("song_file_name"));
					path = reader.GetString(reader.GetOrdinal("folder_path")) + Path.DirectorySeparatorChar + filename;

					long timestamp = DateTime.Now.ToUniversalUnixTimestamp();
					bool exists = File.Exists(path);
					totalExistsTime += DateTime.Now.ToUniversalUnixTimestamp() - timestamp;

					if (!exists)
					{
						orphanSongIds.Add(songid);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed checking for orphan songs " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			foreach (int id in orphanSongIds)
			{
				try
				{
					conn = Database.GetDbConnection();
					IDbCommand q1 = Database.GetDbCommand("DELETE FROM song WHERE song_id = @songid", conn);
					q1.AddNamedParam("@songid", id);
					q1.Prepare();
					q1.ExecuteNonQueryLogged();
					if (logger.IsInfoEnabled) logger.Info("Song " + id + " deleted");
				}
				catch (Exception e)
				{
					logger.Error("Failed deleting orphan songs : " + e);
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		private void CheckArtists()
		{
			if (isRestart)
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;
			ArrayList orphanArtistIds = new ArrayList();

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT artist.artist_id " +
													 "FROM artist " + 
													 "LEFT JOIN song ON artist.artist_id = song.song_artist_id " +
													 "WHERE song.song_artist_id IS NULL", conn);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					orphanArtistIds.Add(reader.GetInt32(0));
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed checking for orphan artists : " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			foreach (int id in orphanArtistIds)
			{
				try
				{
					conn = Database.GetDbConnection();
					IDbCommand q1 = Database.GetDbCommand("DELETE FROM artist WHERE artist_id = @artistid", conn);
					q1.AddNamedParam("@artistid", id);
					q1.Prepare();
					q1.ExecuteNonQueryLogged();
					if (logger.IsInfoEnabled) logger.Info("Artist " + id + " deleted");
				}
				catch (Exception e)
				{
					logger.Error("Failed deleting orphan artists" + e);
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		private void CheckAlbums()
		{
			if (isRestart)
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;
			ArrayList orphanAlbumIds = new ArrayList();

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT album.album_id " +
													 "FROM album " + 
													 "LEFT JOIN song ON album.album_id = song.song_album_id " +
													 "WHERE song.song_album_id IS NULL", conn);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					orphanAlbumIds.Add(reader.GetInt32(0));
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed checking for orphan albums" + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			foreach (int id in orphanAlbumIds)
			{
				try
				{
					conn = Database.GetDbConnection();
					IDbCommand q1 = Database.GetDbCommand("DELETE FROM album WHERE album_id = @albumid", conn);
					q1.AddNamedParam("@albumid", id);
					q1.Prepare();
					q1.ExecuteNonQueryLogged();
					if (logger.IsInfoEnabled) logger.Info("Album " + id + " deleted");
				}
				catch (Exception e)
				{
					logger.Error("Failed deleting orphan albums " + e);
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		private void CheckGenres()
		{
			if (isRestart)
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;
			ArrayList orphanGenreIds = new ArrayList();

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT genre.genre_id " +
													 "FROM genre " +
													 "LEFT JOIN song ON genre.genre_id = song.song_genre_id " +
													 "WHERE song.song_genre_id IS NULL", conn);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					orphanGenreIds.Add(reader.GetInt32(0));
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed checking for orphan genres : " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			foreach (int id in orphanGenreIds)
			{
				try
				{
					conn = Database.GetDbConnection();
					IDbCommand q1 = Database.GetDbCommand("DELETE FROM genre WHERE genre_id = @genreid", conn);
					q1.AddNamedParam("@genreid", id);
					q1.Prepare();
					q1.ExecuteNonQueryLogged();
					if (logger.IsInfoEnabled) logger.Info("Genre " + id + " deleted");
				}
				catch (Exception e)
				{
					logger.Error("Failed deleting orphan genres : " + e);
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}
	}
}
