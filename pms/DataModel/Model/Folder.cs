using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using pms.DataModel.Singletons;
using pms.DataModel.Model;
using System.IO;

namespace pms.DataModel.Model
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

				string query =  string.Format("SELECT folder.*, item_type_art.art_id FROM folder ") +
								string.Format("LEFT JOIN song ON song_folder_id = folder_id ") +
								string.Format("LEFT JOIN item_type_art ON item_type_art.item_type_id = {0} AND item_id = song_id ", new Song().ItemTypeId) +
								string.Format("WHERE folder_id = {0} ", folderId) +
								string.Format("GROUP BY folder_id, item_type_art.art_id");

				var q = new SqlCeCommand(query);
				q.Connection = conn;
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
				Database.close(conn, reader);
			}
		}

		public Folder(string path)
		{
			if (path == null || path == "")
			{
				return;
			}

			_folderPath = path;

			_folderPath = File.g
		}


	}
}
