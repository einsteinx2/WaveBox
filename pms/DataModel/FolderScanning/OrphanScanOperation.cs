using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaFerry.DataModel.Singletons;
using MediaFerry.DataModel.Model;
using System.Data.SqlServerCe;
using System.IO;
using System.Diagnostics;

namespace MediaFerry.DataModel.FolderScanning
{
	class OrphanScanOperation : ScanOperation
	{
		public OrphanScanOperation(int secondsDelay)
		{
		}

		public override void start()
		{
			var sw = new Stopwatch();
			sw.Start();
			checkFolders();
			sw.Stop();
			Console.WriteLine("check folders: {0}ms", sw.ElapsedMilliseconds);
			sw.Restart();
			checkSongs();
			sw.Stop();
			Console.WriteLine("check songs: {0}ms", sw.ElapsedMilliseconds);
		}

		public void checkFolders()
		{
			if (IsRestart)
			{
				return;
			}

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			var mediaFolderIds = new ArrayList();
			var orphanFolderIds = new ArrayList();

			foreach (Folder mediaFolder in Settings.MediaFolders)
			{
				mediaFolderIds.Add(mediaFolder.FolderId);
			}

			try
			{
				conn = Database.getDbConnection();

				var q = new SqlCeCommand("SELECT * FROM folder");

				Database.dbLock.WaitOne();
				q.Connection = conn;

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					// storage for stuff we'll get.
					string path;
					int folderId, mediaFolderId;

					// get ordinals
					int pathOrdinal = reader.GetOrdinal("folder_path");
					int folderIdOrdinal = reader.GetOrdinal("folder_id");
					int mediaFolderIdOrdinal = reader.GetOrdinal("folder_media_folder_id");

					if (reader.GetValue(pathOrdinal) != DBNull.Value)
					{
						path = reader.GetString(reader.GetOrdinal("folder_path"));
					}
					else path = "";

					if (reader.GetValue(folderIdOrdinal) != DBNull.Value)
					{
						folderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
					}
					else folderId = 0;

					if ((int)reader.GetValue(mediaFolderIdOrdinal) != 0)
					{
						mediaFolderId = reader.GetInt32(reader.GetOrdinal("folder_media_folder_id"));
					}
					else mediaFolderId = 0;

					if (mediaFolderId != 0)
					{
						if(!mediaFolderIds.Contains(mediaFolderId) || !Directory.Exists(path))
						{
							Console.WriteLine("[ORPHANSCAN] " + "{0} is orphaned", folderId);
							orphanFolderIds.Add(folderId);
						}
					}
				}

				reader.Close();

				foreach (int fid in orphanFolderIds)
				{
					try
					{
						var q1 = new SqlCeCommand("DELETE FROM folder WHERE folder_id = @folderid", conn);
						q1.Parameters.AddWithValue("@folderid", fid);

						q1.Prepare();
						q1.ExecuteNonQuery();
					}

					catch (Exception e)
					{
						Console.WriteLine("[ORPHANSCAN] " + e.ToString());
					}

					try
					{
						Console.WriteLine("[ORPHANSCAN] " + "Songs for {0} deleted", fid);

						var q2 = new SqlCeCommand("DELETE FROM song WHERE song_folder_id = @folderid", conn);
						q2.Parameters.AddWithValue("@folderid", fid);

						q2.Prepare();
						q2.ExecuteNonQuery();
					}

					catch (Exception e)
					{
						Console.WriteLine("[ORPHANSCAN] " + e.ToString());
					}

				}
			}

			catch (Exception e)
			{
				Console.WriteLine("[ORPHANSCAN] " + e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public void checkSongs()
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			var orphanSongIds = new ArrayList();
			int songid;
			string path, filename;

			try
			{
				var q = new SqlCeCommand("SELECT song.song_id, song.song_file_name, folder.folder_path " +
										 "FROM song " + 
										 "LEFT JOIN folder ON song.song_folder_id = folder.folder_id");

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					songid = reader.GetInt32(reader.GetOrdinal("song_id"));
					filename = reader.GetString(reader.GetOrdinal("song_file_name"));
					path = reader.GetString(reader.GetOrdinal("folder_path")) + Path.DirectorySeparatorChar + filename;


					if (!File.Exists(path))
					{
						orphanSongIds.Add(songid);
					}
				}

				reader.Close();

				foreach (int id in orphanSongIds)
				{
					try
					{
						var q1 = new SqlCeCommand("DELETE FROM song WHERE song_id = @songid", conn);
						q1.Parameters.AddWithValue("@songid", id);
						q1.Prepare();
						q1.ExecuteNonQuery();
						Console.WriteLine("[ORPHANSCAN] " + "Song " + id + " deleted");
						reader.Close();
					}

					catch (Exception e)
					{
						Console.WriteLine("[ORPHANSCAN] " + e.ToString());
					}
				}
			}

			catch (Exception e)
			{
				Console.WriteLine("[ORPHANSCAN] " + e.ToString());
			}

			finally
			{
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}
	}
}
