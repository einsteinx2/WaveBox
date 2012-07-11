using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using System.Data.SqlTypes;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using System.IO;

namespace WaveBox.DataModel.Model
{
	public class Folder
	{
		private int _folderId;
		[JsonProperty("folderId")]
		public int FolderId
		{
			get
			{
				return _folderId;
			}

			set
			{
				_folderId = value;
			}
		}

		private string _folderName;
		[JsonProperty("folderName")]
		public string FolderName
		{
			get
			{
				return _folderName;
			}

			set
			{
				_folderName = value;
			}
		}

		private int _parentFolderId;
		[JsonProperty("parentFolderId")]
		public int ParentFolderId
		{
			get
			{
				return _parentFolderId;
			}

			set
			{
				_parentFolderId = value;
			}
		}

		private int _mediaFolderId;
		[JsonProperty("mediaFolderId")]
		public int MediaFolderId
		{
			get
			{
				return _mediaFolderId;
			}

			set
			{
				_mediaFolderId = value;
			}
		}

		private string _folderPath;
		[JsonProperty("folderPath")]
		public string FolderPath
		{
			get
			{
				return _folderPath;
			}

			set
			{
				_folderPath = value;
			}
		}

		private int _artId;
		[JsonProperty("artId")]
		public int ArtId
		{
			get
			{
				return _artId;
			}

			set
			{
				_artId = value;
			}
		}

		/// <summary>
		/// Constructors
		/// </summary>

		public Folder(int folderId)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				conn = Database.getDbConnection();

				var q = new SqlCeCommand("SELECT folder.*, item_type_art.art_id FROM folder " +
										 "LEFT JOIN song ON song_folder_id = folder_id " +
										 "LEFT JOIN item_type_art ON item_type_art.item_type_id = @itemtypeid AND item_id = song_id " +
										 "WHERE folder_id = @folderid ");

				Database.dbLock.WaitOne();
				q.Connection = conn;
				q.Parameters.AddWithValue("@itemtypeid", new Song().ItemTypeId);
				q.Parameters.AddWithValue("@folderid", folderId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_folderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
					_folderName = reader.GetString(reader.GetOrdinal("folder_name"));
					_folderPath = reader.GetString(reader.GetOrdinal("folder_path"));
					if (reader.GetValue(reader.GetOrdinal("parent_folder_id")) == DBNull.Value)
						_parentFolderId = 0;
					else _parentFolderId = reader.GetInt32(reader.GetOrdinal("parent_folder_id"));
					_mediaFolderId = reader.GetInt32(reader.GetOrdinal("folder_media_folder_id"));

					if (reader.GetValue(reader.GetOrdinal("art_id")) == DBNull.Value)
						_artId = 0;
					else _artId = reader.GetInt32(reader.GetOrdinal("art_id"));
				}
			}

			catch (Exception e)
			{
				Console.WriteLine("[FOLDER] " + e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				Database.close(conn, reader);
			}
		}

		public Folder(string path)
		{
			SqlCeConnection conn = null;
			SqlCeCommand q = null;
			SqlCeDataReader reader = null;

			if (path == null || path == "")
			{
				return;
			}

			_folderPath = path;
			_folderName = Path.GetFileName(path);

			foreach (Folder mf in mediaFolders())
			{
				if (path.Contains(mf.FolderPath))
				{
					MediaFolderId = mf.FolderId;
				}
			}

			try
			{
				q = new SqlCeCommand();

				if (isMediaFolder() || Settings.MediaFolders == null)
				{
					q.CommandText = "SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id IS NULL";
					q.Parameters.AddWithValue("@foldername", FolderName);
				}

				else
				{
					ParentFolderId = _getParentFolderId(FolderPath);
					q.CommandText = "SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id = @parentfolderid";
					q.Parameters.AddWithValue("@foldername", FolderName);
					q.Parameters.AddWithValue("@parentfolderid", ParentFolderId);
				}

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_folderId = reader.GetInt32(0);
				}

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine("[FOLDER] " + e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				Database.close(conn, reader);
			}
		}
		

		public Folder(string path, bool mediafolder)
		{
			FolderPath = path;
			FolderName = Path.GetFileName(path);
			ParentFolderId = 0;
			MediaFolderId = 0;
			FolderId = 0;

			SqlCeConnection conn = null;
			SqlCeCommand q = null;
			SqlCeDataReader reader = null;

			if (path == null || path == "")
			{
				return;
			}

			try
			{
				conn = Database.getDbConnection();

				q = new SqlCeCommand("SELECT * FROM folder WHERE folder_path = @folderpath AND folder_media_folder_id = 0", conn);

				Database.dbLock.WaitOne();
				q.Parameters.AddWithValue("@folderpath", path);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					if (path == reader.GetString(reader.GetOrdinal("folder_path")))
					{
						_folderId = reader.GetInt32(reader.GetOrdinal("folder_id"));
						_folderName = reader.GetString(reader.GetOrdinal("folder_name"));
						_folderName = reader.GetString(reader.GetOrdinal("folder_path"));

						if (reader.GetValue(reader.GetOrdinal("parent_folder_id")) == DBNull.Value)
							_parentFolderId = 0;
						else _parentFolderId = reader.GetInt32(reader.GetOrdinal("parent_folder_id"));
						_mediaFolderId = reader.GetInt32(reader.GetOrdinal("folder_media_folder_id"));
					}

				}
			}

			catch (Exception e)
			{
				Console.WriteLine("[FOLDER] " + e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				Database.close(conn, reader);
			}


		}

		public Folder parentFolder()
		{
			return new Folder(ParentFolderId);
		}



		public void scan()
		{
			// TO DO: scanning!  yay!
		}

		public List<Song> listOfSongs()
		{
			var songs = new List<Song>();

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT song.*, item_type_art.art_id, artist.artist_name, album.album_name FROM song " +
										 "LEFT JOIN item_type_art ON item_type_art.item_type_id = @itemtypeid AND item_id = song_id " +
										 "LEFT JOIN artist ON song_artist_id = artist.artist_id " +
										 "LEFT JOIN album ON song_album_id = album.album_id " +
										 "WHERE song_folder_id = @folderid");

				q.Parameters.AddWithValue("@itemtypeid", new Song().ItemTypeId);
				q.Parameters.AddWithValue("@folderid", FolderId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
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
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				Database.close(conn, reader);
			}

			return songs;
		}

		public List<Folder> listOfSubFolders()
		{
			var folders = new List<Folder>();

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT folder_id FROM folder WHERE parent_folder_id = @folderid");
				q.Parameters.AddWithValue("@folderid", FolderId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
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
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				Database.close(conn, reader);
			}

			return folders;
		}

		bool isMediaFolder()
		{
			Folder mFolder = mediaFolder();

			if (mFolder != null)
			{
				return true;
			}
			else return false;
		}

		private Folder mediaFolder()
		{
			foreach (Folder mediaFolder in Folder.mediaFolders())
			{
				if (FolderPath == mediaFolder.FolderPath)
				{
					return mediaFolder;
				}
			}

			return null;
		}

		public void addToDatabase(bool mediaf)
		{

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			int affected;

			try
			{
				var q = new SqlCeCommand();


				q.CommandText = "INSERT INTO folder (folder_name, folder_path, parent_folder_id, folder_media_folder_id) VALUES (@foldername, @folderpath, @parentfolderid, @folderid)";
				q.Parameters.AddWithValue("@foldername", FolderName);
				q.Parameters.AddWithValue("@folderpath", FolderPath);
				if (mediaf == true)
				{
					q.Parameters.AddWithValue("@parentfolderid", DBNull.Value);
				}
				else
				{
					q.Parameters.AddWithValue("@parentfolderid", _getParentFolderId(FolderPath));
				}
				q.Parameters.AddWithValue("@folderid", MediaFolderId);


				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				affected = q.ExecuteNonQuery();

				// get the id of the previous insert.  weird.
				q.CommandText = "SELECT @@IDENTITY";
				FolderId = Convert.ToInt32(q.ExecuteScalar().ToString());
				//FolderId = Convert.ToInt32(((SqlDecimal)q.ExecuteScalar()).ToString());
			}

			catch (Exception e)
			{
				Console.WriteLine("[FOLDER] " + e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				Database.close(conn, reader);
			}
		}

		private int _getParentFolderId(string path)
		{
			string parentFolderPath = Directory.GetParent(path).FullName;

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			int pFolderId = 0;

			try
			{
				var q = new SqlCeCommand("SELECT folder_id FROM folder WHERE folder_path = @folderpath");
				q.Parameters.AddWithValue("@folderpath", parentFolderPath);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				var result = q.ExecuteScalar();

				if(result == null)
				{
					Console.WriteLine("No db result for parent folder.  Constructing parent folder object.");
					var f = new Folder(parentFolderPath);
					f.addToDatabase(false);
					pFolderId = f.FolderId;
				}

				else pFolderId = (int)result;
				
			}

			catch (Exception e)
			{
				Console.WriteLine("[FOLDER] " + e.ToString());
			}

			finally
			{
				try
				{
					Database.dbLock.ReleaseMutex();
				}

				catch (Exception e)
				{
					Console.WriteLine("[FOLDER] " + e.ToString());
				}
				Database.close(conn, reader);
			}

			return pFolderId;
		}

		public static List<Folder> mediaFolders()
		{
			var folders = new List<Folder>();
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			if (Settings.MediaFolders == null)
			{

				try
				{
					var q = new SqlCeCommand("SELECT * FROM folder WHERE parent_folder_id IS NULL");
					//q.Parameters.AddWithValue("@nullvalue", DBNull.Value);

					Database.dbLock.WaitOne();
					conn = Database.getDbConnection();
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
					try
					{
						Database.dbLock.ReleaseMutex();
					}

					catch (Exception e)
					{
						Console.WriteLine("[FOLDER] " + e.ToString());
					}
					Database.close(conn, reader);
				}
			}

			else return Settings.MediaFolders;

			return folders;
		}

		public static List<Folder> topLevelFolders()
		{
			var folders = new List<Folder>();

			foreach (Folder mediaFolder in mediaFolders())
			{
				folders.AddRange(mediaFolder.listOfSubFolders());
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
