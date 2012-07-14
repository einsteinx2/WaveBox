using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using System.Data.SQLite;
using System.IO;
using System.Diagnostics;

namespace WaveBox.DataModel.FolderScanning
{
	class OrphanScanOperation : ScanOperation
	{
		public OrphanScanOperation(int secondsDelay) : base(secondsDelay)
		{
		}

		public override void Start()
		{
			var sw = new Stopwatch();
			sw.Start();
			CheckFolders();
			sw.Stop();
			Console.WriteLine("[ORPHANSCAN] check folders: {0}ms", sw.ElapsedMilliseconds);
			sw.Restart();
			CheckSongs();
			sw.Stop();
			Console.WriteLine("[ORPHANSCAN] check songs: {0}ms", sw.ElapsedMilliseconds);
		}

		public void CheckFolders ()
		{
			if (ShouldRestart) {
				return;
			}

			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;
			var mediaFolderIds = new ArrayList ();
			var orphanFolderIds = new ArrayList ();

			foreach (Folder mediaFolder in Settings.MediaFolders) {
				mediaFolderIds.Add (mediaFolder.FolderId);
			}

			lock (Database.dbLock) 
			{
				try 
				{
					conn = Database.GetDbConnection ();

					var q = new SQLiteCommand ("SELECT * FROM folder");

					q.Connection = conn;

					q.Prepare ();
					reader = q.ExecuteReader ();

					while (reader.Read()) 
					{
						// storage for stuff we'll get.
						string path;
						int folderId, mediaFolderId;

						// get ordinals
						int pathOrdinal = reader.GetOrdinal ("folder_path");
						int folderIdOrdinal = reader.GetOrdinal ("folder_id");
						int mediaFolderIdOrdinal = reader.GetOrdinal ("folder_media_folder_id");

						if (reader.GetValue (pathOrdinal) != DBNull.Value) 
						{
							path = reader.GetString (reader.GetOrdinal ("folder_path"));
						} 
						else
						{
							path = "";
						}

						if (reader.GetValue (folderIdOrdinal) != DBNull.Value)
						{
							folderId = reader.GetInt32 (reader.GetOrdinal ("folder_id"));
						} 
						else 
						{
							folderId = 0;
						}

						if ((long)reader.GetValue (mediaFolderIdOrdinal) != 0) 
						{
							mediaFolderId = reader.GetInt32 (reader.GetOrdinal ("folder_media_folder_id"));
						} 
						else
						{
							mediaFolderId = 0;
						}

						if (mediaFolderId != 0)
						{
							if (!mediaFolderIds.Contains (mediaFolderId) || !Directory.Exists (path)) 
							{
								Console.WriteLine ("[ORPHANSCAN] " + "{0} is orphaned", folderId);
								orphanFolderIds.Add (folderId);
							}
						}
					}

					reader.Close ();

					foreach (int fid in orphanFolderIds) 
					{
						try 
						{
							var q1 = new SQLiteCommand ("DELETE FROM folder WHERE folder_id = @folderid", conn);
							q1.Parameters.AddWithValue ("@folderid", fid);

							q1.Prepare ();
							q1.ExecuteNonQuery ();
						} 
						catch (Exception e) 
						{
							Console.WriteLine ("[ORPHANSCAN] " + e.ToString ());
						}

						try
						{
							Console.WriteLine ("[ORPHANSCAN] " + "Songs for {0} deleted", fid);

							var q2 = new SQLiteCommand ("DELETE FROM song WHERE song_folder_id = @folderid", conn);
							q2.Parameters.AddWithValue ("@folderid", fid);

							q2.Prepare ();
							q2.ExecuteNonQuery ();
						} 
						catch (Exception e) 
						{
							Console.WriteLine ("[ORPHANSCAN] " + e.ToString ());
						}

					}
				}
				catch (Exception e) 
				{
					Console.WriteLine ("[ORPHANSCAN] " + e.ToString ());
				}
				finally
				{
					Database.Close (conn, reader);
				}
			}
		}

		public void CheckSongs()
		{
			if (ShouldRestart)
			{
				return;
			}

			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;
			var orphanSongIds = new ArrayList();
			int songid;
			string path, filename;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SQLiteCommand("SELECT song.song_id, song.song_file_name, folder.folder_path " +
						"FROM song " + 
						"LEFT JOIN folder ON song.song_folder_id = folder.folder_id"
					);

					conn = Database.GetDbConnection();
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
							var q1 = new SQLiteCommand("DELETE FROM song WHERE song_id = @songid", conn);
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
					Database.Close(conn, reader);
				}
			}
		}

		public override string ScanType()
		{
			return "OrphanScanOperation";
		}
	}
}
