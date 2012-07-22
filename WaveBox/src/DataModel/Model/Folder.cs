using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
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

        public bool ContainsImageFile { get; set; }


		/// <summary>
		/// Constructors
		/// </summary>

		public Folder(int folderId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();

				IDbCommand q = Database.GetDbCommand("SELECT folder.*, song.song_art_id FROM folder " + 
					"LEFT JOIN song ON song_folder_id = folder_id " +
					"WHERE folder_id = @folderid LIMIT 1", conn);

				q.AddNamedParam("@folderid", folderId);
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
                        ContainsImageFile = false;
						if (reader.GetValue(reader.GetOrdinal("song_art_id")) == DBNull.Value)
							ArtId = 0;
						else 
							ArtId = reader.GetInt32(reader.GetOrdinal("song_art_id"));
					}
					else
					{
                        ContainsImageFile = true;
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

		public Folder(string path)
		{
			IDbConnection conn = null;
			IDbCommand q = null;
			IDataReader reader = null;

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

			try
			{
				conn = Database.GetDbConnection();
				if (IsMediaFolder() || Settings.MediaFolders == null)
				{
					q = Database.GetDbCommand("SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id IS NULL", conn);
					q.AddNamedParam("@foldername", FolderName);
				}
				else
				{
					ParentFolderId = GetParentFolderId(FolderPath);
					q = Database.GetDbCommand("SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id = @parentfolderid", conn);
					q.AddNamedParam("@foldername", FolderName);
					q.AddNamedParam("@parentfolderid", ParentFolderId);
				}

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					FolderId = reader.GetInt32(0);
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
		

		public Folder(string path, bool mediafolder)
		{
			FolderPath = path;
			FolderName = Path.GetFileName(path);
			ParentFolderId = 0;
			MediaFolderId = 0;
			FolderId = 0;

			IDbCommand q = null;
			IDbConnection conn = null;
			IDataReader reader = null;

			if (path == null || path == "")
			{
				return;
			}

			try
			{
				conn = Database.GetDbConnection();
				q = Database.GetDbCommand("SELECT * FROM folder WHERE folder_path = \"" + path + "\" AND folder_media_folder_id = 0", conn);
				//q = Database.GetDbCommand("SELECT * FROM folder WHERE folder_path = @folderpath AND folder_media_folder_id = 0", conn);
				//q.AddNamedParam("@folderpath", path);
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
				reader.Close();
				conn.Close();
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

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
                conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " +
					"LEFT JOIN artist ON song_artist_id = artist.artist_id " +
					"LEFT JOIN album ON song_album_id = album.album_id " +
				    "WHERE song_folder_id = @folderid", conn);

				q.AddNamedParam("@folderid", FolderId);


				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					songs.Add(new Song(reader));
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

			songs.Sort(Song.CompareSongsByDiscAndTrack);
			return songs;
		}

		public List<Folder> ListOfSubFolders()
		{
			var folders = new List<Folder>();

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT folder_id FROM folder WHERE parent_folder_id = @folderid", conn);
				q.AddNamedParam("@folderid", FolderId);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					folders.Add(new Folder(reader.GetInt32(0)));
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
                    ContainsImageFile = true;
					return true;
				}
			}

			firstImageFoundPath = "";
            ContainsImageFile = false;
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
			IDbConnection conn = null;
			IDataReader reader = null;
			int affected;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO folder (folder_name, folder_path, parent_folder_id, folder_media_folder_id, folder_art_id) " +
													 "VALUES (@foldername, @folderpath, @parentfolderid, @folderid, @artid)", conn);

				q.AddNamedParam("@foldername", FolderName);
				q.AddNamedParam("@folderpath", FolderPath);

				if (mediaf == true)
					q.AddNamedParam("@parentfolderid", DBNull.Value);
				else
					q.AddNamedParam("@parentfolderid", GetParentFolderId(FolderPath));

				q.AddNamedParam("@folderid", MediaFolderId);

				if (ArtId == 0) 
					q.AddNamedParam("@artid", DBNull.Value);
				else 
					q.AddNamedParam("@artid", ArtId);
				
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

		private int GetParentFolderId(string path)
		{
			string parentFolderPath = Directory.GetParent(path).FullName;

			IDbConnection conn = null;
			IDataReader reader = null;
			int pFolderId = 0;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT folder_id FROM folder WHERE folder_path = @folderpath", conn);
				q.AddNamedParam("@folderpath", parentFolderPath);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					pFolderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
				}
				else
				{
					Console.WriteLine("No db result for parent folder.  Constructing parent folder object.");
					var f = new Folder(parentFolderPath);
					f.AddToDatabase(false);
					pFolderId = f.FolderId;
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

			return pFolderId;
		}

		public static List<Folder> MediaFolders ()
		{
			var folders = new List<Folder> ();
			IDbConnection conn = null;
			IDataReader reader = null;

			if (Settings.MediaFolders == null) 
			{
				try 
				{
					conn = Database.GetDbConnection();
					IDbCommand q = Database.GetDbCommand ("SELECT * FROM folder WHERE parent_folder_id IS NULL", conn);
					//q.Parameters.AddWithValue("@nullvalue", DBNull.Value);
					q.Prepare();
					reader = q.ExecuteReader();

					while (reader.Read())
					{
						folders.Add (new Folder(reader.GetInt32(reader.GetOrdinal("folder_id"))));
					}
				} 
				catch (Exception e) 
				{
					Console.WriteLine ("[FOLDER] " + e.ToString());
				} 
				finally
				{
					Database.Close(conn, reader);
				}
			} 
			else
			{
				return Settings.MediaFolders;
			}

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
