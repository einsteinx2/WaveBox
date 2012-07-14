using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Data.SqlTypes;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using System.IO;

namespace WaveBox.DataModel.Model
{
	public class Folder
	{
		[JsonProperty("folderId")]
		public int FolderId { get; set; }

		[JsonProperty("folderName")]
		public string FolderName { get; set; }

		[JsonProperty("parentFolderId")]
		public int ParentFolderId { get; set; }

		[JsonProperty("mediaFolderId")]
		public int MediaFolderId { get; set; }

		[JsonProperty("folderPath")]
		public string FolderPath { get; set; }

		[JsonProperty("artId")]
		public int ArtId { get; set; }


		/// <summary>
		/// Constructors
		/// </summary>

		public Folder(int folderId)
		{
			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();

					var q = new SQLiteCommand("SELECT TOP(1) folder.*, song.song_art_id FROM folder " + 
						"LEFT JOIN song ON song_folder_id = folder_id " +
						"WHERE folder_id = @folderid"
					);

					q.Connection = conn;
					q.Parameters.AddWithValue("@folderid", folderId);
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						FolderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
						FolderName = reader.GetString(reader.GetOrdinal("folder_name"));
						FolderPath = reader.GetString(reader.GetOrdinal("folder_path"));
						if (reader.GetValue(reader.GetOrdinal("parent_folder_id")) == DBNull.Value)
							ParentFolderId = 0;
						else 
							ParentFolderId = reader.GetInt32(reader.GetOrdinal("parent_folder_id"));
						MediaFolderId = reader.GetInt32(reader.GetOrdinal("folder_media_folder_id"));

						// if the folder has no associated art, i.e. there is no image file in the folder,
						// check to see if there was a song image available, i.e. there was an image in its tag
						// if neither of these things, then sadly, we have no image file to show the user.
						if (reader.GetValue(reader.GetOrdinal("folder_art_id")) == DBNull.Value)
						{
							if (reader.GetValue(reader.GetOrdinal("song_art_id")) == DBNull.Value)
								ArtId = 0;
							else 
								ArtId = reader.GetInt32(reader.GetOrdinal("song_art_id"));
						}
						else
						{
							ArtId = reader.GetInt32(reader.GetOrdinal("folder_art_id"));
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		public Folder(string path)
		{
			SQLiteConnection conn = null;
			SQLiteCommand q = null;
			SQLiteDataReader reader = null;

			if (path == null || path == "")
			{
				return;
			}

			FolderPath = path;
			FolderName = Path.GetFileName(path);

			foreach (Folder mf in MediaFolders())
			{
				if (path.Contains(mf.FolderPath))
				{
					MediaFolderId = mf.FolderId;
				}
			}

			string folderImageName;
			if (FolderContainsImages(path, out folderImageName))
			{
				ArtId = new CoverArt(new FileStream(folderImageName, FileMode.Open, FileAccess.Read)).ArtId;
			}

			lock (Database.dbLock)
			{
				try
				{
					q = new SQLiteCommand();

					if (IsMediaFolder() || Settings.MediaFolders == null)
					{
						q.CommandText = "SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id IS NULL";
						q.Parameters.AddWithValue("@foldername", FolderName);
					}
					else
					{
						ParentFolderId = GetParentFolderId(FolderPath);
						q.CommandText = "SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id = @parentfolderid";
						q.Parameters.AddWithValue("@foldername", FolderName);
						q.Parameters.AddWithValue("@parentfolderid", ParentFolderId);
					}

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						FolderId = reader.GetInt32(0);
					}

					reader.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}
		

		public Folder(string path, bool mediafolder)
		{
			FolderPath = path;
			FolderName = Path.GetFileName(path);
			ParentFolderId = 0;
			MediaFolderId = 0;
			FolderId = 0;

			SQLiteConnection conn = null;
			SQLiteCommand q = null;
			SQLiteDataReader reader = null;

			if (path == null || path == "")
			{
				return;
			}

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();

					q = new SQLiteCommand("SELECT * FROM folder WHERE folder_path = @folderpath AND folder_media_folder_id = 0", conn);

					q.Parameters.AddWithValue("@folderpath", path);
					q.Prepare();
					reader = q.ExecuteReader();

					while (reader.Read())
					{
						if (path == reader.GetString(reader.GetOrdinal("folder_path")))
						{
							FolderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
							FolderName = reader.GetString(reader.GetOrdinal("folder_name"));
							FolderName = reader.GetString(reader.GetOrdinal("folder_path"));

							if (reader.GetValue(reader.GetOrdinal("parent_folder_id")) == DBNull.Value)
								ParentFolderId = 0;
							else 
								ParentFolderId = reader.GetInt32(reader.GetOrdinal("parent_folder_id"));
							MediaFolderId = reader.GetInt32(reader.GetOrdinal("folder_media_folder_id"));

							if (reader.GetValue(reader.GetOrdinal("folder_art_id")) == DBNull.Value)
								ArtId = 0;
							else 
								ParentFolderId = reader.GetInt32(reader.GetOrdinal("folder_art_id"));
						}

					}
				}
				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}

		}

		public Folder ParentFolder()
		{
			return new Folder(ParentFolderId);
		}



		public void Scan()
		{
			// TO DO: scanning!  yay!
		}

		public List<Song> ListOfSongs()
		{
			var songs = new List<Song>();

			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SQLiteCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " +
						"LEFT JOIN artist ON song_artist_id = artist.artist_id " +
						"LEFT JOIN album ON song_album_id = album.album_id " +
						"WHERE song_folder_id = @folderid"
					);

					q.Parameters.AddWithValue("@folderid", FolderId);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();

					while (reader.Read())
					{
						songs.Add(new Song(reader));
					}

					reader.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}

			songs.Sort(Song.CompareSongsByDiscAndTrack);
			return songs;
		}

		public List<Folder> ListOfSubFolders()
		{
			var folders = new List<Folder>();

			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SQLiteCommand("SELECT folder_id FROM folder WHERE parent_folder_id = @folderid");
					q.Parameters.AddWithValue("@folderid", FolderId);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();

					while (reader.Read())
					{
						folders.Add(new Folder(reader.GetInt32(0)));
					}

					reader.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}

			folders.Sort(CompareFolderByName);
			return folders;
		}

		private bool IsMediaFolder()
		{
			Folder mFolder = MediaFolder();

			if (mFolder != null)
			{
				return true;
			}
			else return false;
		}

		private bool FolderContainsImages(string dir, out string firstImageFoundPath)
		{
			var validImageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
			string ext = "";

			foreach (string file in Directory.GetFiles(dir))
			{
				ext = Path.GetExtension(file).ToLower();
				if (validImageExtensions.Contains(ext))
				{
					firstImageFoundPath = file;
					return true;
				}
			}

			firstImageFoundPath = "";
			return false;
		}

		private Folder MediaFolder()
		{
			foreach (Folder mediaFolder in Folder.MediaFolders())
			{
				if (FolderPath == mediaFolder.FolderPath)
				{
					return mediaFolder;
				}
			}

			return null;
		}

		public void AddToDatabase(bool mediaf)
		{
			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;
			int affected;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SQLiteCommand();

					q.CommandText = "INSERT INTO folder (folder_name, folder_path, parent_folder_id, folder_media_folder_id, folder_art_id) VALUES (@foldername, @folderpath, @parentfolderid, @folderid, @artid)";
					q.Parameters.AddWithValue("@foldername", FolderName);
					q.Parameters.AddWithValue("@folderpath", FolderPath);
					if (mediaf == true)
					{
						q.Parameters.AddWithValue("@parentfolderid", DBNull.Value);
					}
					else
					{
						q.Parameters.AddWithValue("@parentfolderid", GetParentFolderId(FolderPath));
					}
					q.Parameters.AddWithValue("@folderid", MediaFolderId);

					if (ArtId == 0) 
						q.Parameters.AddWithValue("@artid", DBNull.Value);
					else 
						q.Parameters.AddWithValue("@artid", ArtId);
					
					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					affected = q.ExecuteNonQuery();

					if (affected > 0)
					{
						// get the id of the previous insert.  weird.
						q.CommandText = "SELECT last_insert_rowid()";
						FolderId = Convert.ToInt32(q.ExecuteScalar().ToString());
						//FolderId = Convert.ToInt32(((SqlDecimal)q.ExecuteScalar()).ToString());
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		private int GetParentFolderId(string path)
		{
			string parentFolderPath = Directory.GetParent(path).FullName;

			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;
			int pFolderId = 0;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SQLiteCommand("SELECT folder_id FROM folder WHERE folder_path = @folderpath");
					q.Parameters.AddWithValue("@folderpath", parentFolderPath);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					var result = q.ExecuteScalar();

					if (result == null)
					{
						Console.WriteLine("No db result for parent folder.  Constructing parent folder object.");
						var f = new Folder(parentFolderPath);
						f.AddToDatabase(false);
						pFolderId = f.FolderId;
					}
					else
					{
						pFolderId = (int)result;
					}
					
				}
				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}

			return pFolderId;
		}

		public static List<Folder> MediaFolders()
		{
			var folders = new List<Folder>();
			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;

			if (Settings.MediaFolders == null)
			{
			
				lock (Database.dbLock)
				{
					try
					{
						var q = new SQLiteCommand("SELECT * FROM folder WHERE parent_folder_id IS NULL");
						//q.Parameters.AddWithValue("@nullvalue", DBNull.Value);

						conn = Database.GetDbConnection();
						q.Connection = conn;
						q.Prepare();
						reader = q.ExecuteReader();

						while (reader.Read())
						{
							folders.Add(new Folder(reader.GetInt32(reader.GetOrdinal("folder_id"))));
						}

						reader.Close();
					}

					catch (Exception e)
					{
						Console.WriteLine("[FOLDER] " + e.ToString());
					}

					finally
					{
						Database.Close(conn, reader);
					}
				}
			}

			else return Settings.MediaFolders;

			return folders;
		}

		public static List<Folder> TopLevelFolders()
		{
			var folders = new List<Folder>();

			foreach (Folder mediaFolder in MediaFolders())
			{
				folders.AddRange(mediaFolder.ListOfSubFolders());
			}

			folders.Sort(CompareFolderByName);
			return folders;
		}

		public static int CompareFolderByName(Folder x, Folder y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.FolderName, y.FolderName);
		}
	}
}
