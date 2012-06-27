using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using System.Data.SqlTypes;
using MediaFerry.DataModel.Singletons;
using MediaFerry.DataModel.Model;
using System.Diagnostics;
using System.IO;

namespace MediaFerry.DataModel.Model
{
	public class Folder
	{
		private int _folderId;
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
										 "WHERE folder_id = @folderid " +
										 "GROUP BY folder_id, item_type_art.art_id");

				Database.dblock.WaitOne();
				q.Connection = conn;
				q.Parameters.AddWithValue("@itemtypeid", new Song().ItemTypeId);
				q.Parameters.AddWithValue("@folderid", folderId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_folderId = reader.GetInt32(0);
					_folderName = reader.GetString(1);
					_folderPath = reader.GetString(2);
					_parentFolderId = reader.GetInt32(3);
					_mediaFolderId = reader.GetInt32(4);
					_artId = reader.GetInt32(5);
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dblock.ReleaseMutex();
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


			try
			{
				q = new SqlCeCommand();

				if (isMediaFolder())
				{
					q.CommandText = "SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id IS NULL";
					q.Parameters.AddWithValue("@foldername", FolderName);
				}

				else
				{
					q.CommandText = "SELECT folder_id FROM folder WHERE folder_name = @foldername AND parent_folder_id = @parentfolderid";
					q.Parameters.AddWithValue("@foldername", FolderName);
					q.Parameters.AddWithValue("@parentfolderid", ParentFolderId);
				}

				Database.dblock.WaitOne();
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
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dblock.ReleaseMutex();
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

				Database.dblock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					songs.Add(new Song(reader));
				}

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dblock.ReleaseMutex();
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
				var q = new SqlCeCommand("SELECT * FROM folder WHERE parent_folder_id = @folderid");
				q.Parameters.AddWithValue("@folderid", FolderId);

				Database.dblock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					folders.Add(new Folder(reader.GetInt32(0)));
				}

				reader.Close();
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dblock.ReleaseMutex();
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
				if (FolderPath.StartsWith(mediaFolder.FolderPath))
				{
					return mediaFolder;
				}
			}

			return null;
		}

		public void addToDatabase()
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;
			int affected;

			try
			{
				Folder mf = mediaFolder();
				var q = new SqlCeCommand();

				if (mf == null)
				{
					q.CommandText = "INSERT INTO folder (folder_name, folder_path, parent_folder_id) VALUES (@foldername, @folderpath, @parentfolderid)";
					q.Parameters.AddWithValue("@foldername", FolderName);
					q.Parameters.AddWithValue("@folderpath", FolderPath);
					q.Parameters.AddWithValue("@parentfolderid", ParentFolderId);
				}

				else
				{
					q.CommandText = "INSERT INTO folder (folder_name, folder_path, parent_folder_id, folder_media_folder_id) VALUES (@foldername, @folderpath, @parentfolderid, @folderid)";
					q.Parameters.AddWithValue("@foldername", FolderName);
					q.Parameters.AddWithValue("@folderpath", FolderPath);
					q.Parameters.AddWithValue("@parentfolderid", ParentFolderId);
					q.Parameters.AddWithValue("@folderid", mf.FolderId);
				}

				Database.dblock.WaitOne();
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
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dblock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public static List<Folder> mediaFolders()
		{
			var folders = new List<Folder>();
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				string query = "SELECT * FROM folder WHERE parent_folder_id = null";
				var q = new SqlCeCommand(query);

				Database.dblock.WaitOne();
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
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.dblock.ReleaseMutex();
				Database.close(conn, reader);
			}

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
