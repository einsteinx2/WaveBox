using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using System.Data;
using System.IO;
using System.Diagnostics;
using WaveBox.OperationQueue;

namespace WaveBox.DataModel.FolderScanning
{
	class OrphanScanOperation : AbstractOperation
	{
		public override string OperationType { get { return "OrphanScanOperation"; } }

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

		public void CheckFolders()
		{
			if (isRestart) 
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;
			var mediaFolderIds = new ArrayList ();
			var orphanFolderIds = new ArrayList ();

			foreach (Folder mediaFolder in Settings.MediaFolders) {
				mediaFolderIds.Add (mediaFolder.FolderId);
			}

			try 
			{
				conn = Database.GetDbConnection ();

				IDbCommand q = Database.GetDbCommand ("SELECT * FROM folder", conn);
				q.Prepare ();
				reader = q.ExecuteReader ();

				while (reader.Read()) 
				{
					// storage for stuff we'll get.
					string path;
					int? folderId, mediaFolderId;

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
						folderId = null;
					}

					if (reader.GetValue (mediaFolderIdOrdinal) != DBNull.Value) 
					{
						mediaFolderId = reader.GetInt32 (reader.GetOrdinal ("folder_media_folder_id"));
					} 
					else
					{
						mediaFolderId = null;
					}

					if (mediaFolderId != null)
					{
						if (!mediaFolderIds.Contains (mediaFolderId) || !Directory.Exists (path)) 
						{
							Console.WriteLine ("[ORPHANSCAN] " + "{0} is orphaned", folderId);
							orphanFolderIds.Add (folderId);
						}
					}
				}

				foreach (int fid in orphanFolderIds) 
				{
					try 
					{
						IDbCommand q1 = Database.GetDbCommand ("DELETE FROM folder WHERE folder_id = @folderid", conn);
						q1.AddNamedParam("@folderid", fid);

						q1.Prepare ();
						q1.ExecuteNonQuery ();
					} 
					catch (Exception e) 
					{
						Console.WriteLine ("[ORPHANSCAN(1)] " + e.ToString ());
					}

					try
					{
						Console.WriteLine ("[ORPHANSCAN] " + "Songs for {0} deleted", fid);

						IDbCommand q2 = Database.GetDbCommand ("DELETE FROM song WHERE song_folder_id = @folderid", conn);
						q2.AddNamedParam("@folderid", fid);

						q2.Prepare ();
						q2.ExecuteNonQuery ();
					} 
					catch (Exception e) 
					{
						Console.WriteLine ("[ORPHANSCAN(2)] " + e.ToString ());
					}

				}
			}
			catch (Exception e) 
			{
				Console.WriteLine ("[ORPHANSCAN(3)] " + e.ToString ());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public void CheckSongs()
		{
			if (isRestart)
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;
			var orphanSongIds = new ArrayList();
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


					if (!File.Exists(path))
					{
						orphanSongIds.Add(songid);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ORPHANSCAN(4)] " + e.ToString());
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
					q1.ExecuteNonQuery();
					Console.WriteLine("[ORPHANSCAN] " + "Song " + id + " deleted");
				}
				catch (Exception e)
				{
					Console.WriteLine("[ORPHANSCAN(5)] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}
	}
}
