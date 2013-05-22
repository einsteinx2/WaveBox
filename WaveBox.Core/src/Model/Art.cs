using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlTypes;
using WaveBox.Static;
using WaveBox.Model;
using System.Security.Cryptography;
using TagLib;
using Newtonsoft.Json;

namespace WaveBox.Model
{
	public class Art
	{
		public static readonly string[] ValidExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Properties
		/// </summary>

		[JsonProperty("artId")]
		public int? ArtId { get; set; }

		[JsonProperty("md5Hash")]
		public string Md5Hash { get; set; }

		[JsonProperty("lastModified")]
		public long? LastModified { get; set; }

		[JsonProperty("fileSize")]
		public long? FileSize { get; set; }

		[JsonIgnore]
		public string FilePath { get; set; }

		[JsonIgnore]
		public Stream Stream { get { return CreateStream(); } }

		/// <summary>
		/// Constructors
		/// </summary>

		public Art()
		{

		}

		public Art(int artId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM art WHERE art_id = @artid", conn);
				q.AddNamedParam("@artid", artId);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					ArtId = reader.GetInt32OrNull(reader.GetOrdinal("art_id"));
					Md5Hash = reader.GetStringOrNull(reader.GetOrdinal("md5_hash"));
					LastModified = reader.GetInt64OrNull(reader.GetOrdinal("art_last_modified"));
					FileSize = reader.GetInt64OrNull(reader.GetOrdinal("art_file_size"));
					FilePath = reader.GetStringOrNull(reader.GetOrdinal("art_file_path"));
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

		// used for getting art from a file.
		public Art(string filePath)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

			// compute the hash of the file stream
			Md5Hash = CalcMd5Hash(fs);
			FileSize = fs.Length;
			LastModified = System.IO.File.GetLastWriteTime(fs.Name).ToUniversalUnixTimestamp();
			ArtId = Art.ArtIdForMd5(Md5Hash);
			FilePath = filePath;

			if ((object)ArtId == null)
			{
				InsertArt();
			}
		}

		// used for getting art from a tag.
		// We don't set the FilePath here, because that is only used for actual art files on disk
		public Art(TagLib.File file)
		{
			if (file.Tag.Pictures.Length > 0)
			{
				byte[] data = file.Tag.Pictures[0].Data.Data;
				Md5Hash = CalcMd5Hash(data);
				FileSize = data.Length;
				LastModified = System.IO.File.GetLastWriteTime(file.Name).ToUniversalUnixTimestamp();

				ArtId = Art.ArtIdForMd5(Md5Hash);
				if (ArtId == null)
				{
					// This art isn't in the database yet, so add it
					InsertArt();
				}
			}
		}

		public void InsertArt()
		{
			int? itemId = Item.GenerateItemId(ItemType.Art);
			if (itemId == null)
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				// insert the song into the database
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO art (art_id, md5_hash, art_last_modified, art_file_size, art_file_path)" + 
													 "VALUES (@artid, @md5hash, @lastmodified, @filesize, @artfilepath)"
													, conn);

				q.AddNamedParam("@artid", itemId);
				q.AddNamedParam("@md5hash", Md5Hash);
				q.AddNamedParam("@lastmodified", LastModified);
				q.AddNamedParam("@filesize", FileSize);
				q.AddNamedParam("@artfilepath", FilePath);
				q.Prepare();

				if (q.ExecuteNonQueryLogged() > 0)
				{
					ArtId = itemId;
				}

				return;
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

		private Stream CreateStream()
		{
			if ((object)ArtId == null)
			{
				return null;
			}

			int? itemId = Art.ItemIdForArtId((int)ArtId);

			if ((object)itemId == null)
			{
				return null;
			}

			ItemType type = Item.ItemTypeForItemId((int)itemId);

			Stream stream = null;

			if (type == ItemType.Song)
			{
				stream = StreamForSong((int)itemId);
			}
			else if (type == ItemType.Folder)
			{
				stream = StreamForFolder((int)itemId);
			}

			return stream;
		}

		private Stream StreamForSong(int songId)
		{
			Song song = new Song(songId);
			Stream stream = null;

			// Open the image from the tag
			TagLib.File f = null;
			try
			{
				f = TagLib.File.Create(song.FilePath);
				byte[] data = f.Tag.Pictures[0].Data.Data;

				stream = new MemoryStream(data);
			}
			catch (TagLib.CorruptFileException e)
			{
				if (logger.IsInfoEnabled) logger.Info(song.FileName + " has a corrupt tag so can't return the art. " + e);
			}
			catch (Exception e)
			{
				logger.Error("Error processing file: ", e);
			}

			return stream;
		}

		private Stream StreamForFolder(int folderId)
		{
			Folder folder = new Folder.Factory().CreateFolder(folderId);
			Stream stream = null;

			string artPath = folder.ArtPath;

			if ((object)artPath != null)
			{
				stream = new FileStream(artPath, FileMode.Open, FileAccess.Read);
			}

			return stream;
		}

		// Based off of example at http://msdn.microsoft.com/en-us/library/s02tk69a.aspx
		static string CalcMd5Hash(byte[] input)
		{
			using (MD5 md5 = MD5.Create())
			{
				// Convert the input string to a byte array and compute the hash. 
				byte[] data = md5.ComputeHash(input);

				// Create a new Stringbuilder to collect the bytes 
				// and create a string.
				StringBuilder sBuilder = new StringBuilder();

				// Loop through each byte of the hashed data  
				// and format each one as a hexadecimal string. 
				for (int i = 0; i < data.Length; i++)
				{
					sBuilder.Append(data[i].ToString("x2"));
				}

				// Return the hexadecimal string. 
				return sBuilder.ToString();
			}
		}

		static string CalcMd5Hash(Stream input)
		{
			using (MD5 md5 = MD5.Create())
			{
				// Convert the input string to a byte array and compute the hash. 
				byte[] data = md5.ComputeHash(input);

				// Create a new Stringbuilder to collect the bytes 
				// and create a string.
				StringBuilder sBuilder = new StringBuilder();

				// Loop through each byte of the hashed data  
				// and format each one as a hexadecimal string. 
				for (int i = 0; i < data.Length; i++)
				{
					sBuilder.Append(data[i].ToString("x2"));
				}

				// Return the hexadecimal string. 
				return sBuilder.ToString();
			}
		}

		public static bool FileNeedsUpdating(string filePath, int? folderId)
		{
			if (filePath == null || folderId == null)
			{
				return false;
			}

			// We don't need to instantiate another folder to know what the folder id is.  This should be known when the method is called.

			//Stopwatch sw = new Stopwatch();
			long lastModified = System.IO.File.GetLastWriteTime(filePath).ToUniversalUnixTimestamp();
			bool needsUpdating = true;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				// Turns out that COUNT(*) on large tables is REALLY slow in SQLite because it does a full table search.  I created an index on folder_id(because weirdly enough,
				// even though it's a primary key, SQLite doesn't automatically make one!  :O).  We'll pull that, and if we get a row back, then we'll know that this thing exists.

				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT art_id FROM art WHERE art_last_modified = @lastmod AND art_file_path = @filepath", conn);
				//IDbCommand q = Database.GetDbCommand("SELECT COUNT(*) AS count FROM song WHERE song_folder_id = @folderid AND song_file_name = @filename AND song_last_modified = @lastmod", conn);

				q.AddNamedParam("@filepath", filePath);
				q.AddNamedParam("@lastmod", lastModified);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					needsUpdating = false;
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

		public static int? ItemIdForArtId(int artId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			int? itemId = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT item_id FROM art_item WHERE art_id = @artid", conn);
				q.AddNamedParam("@artid", artId);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					// Grab the first available item id that is associated with this art id
					// doesn't matter which one because they all have the same art
					itemId = reader.GetInt32(0);
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

			return itemId;
		}

		public static int? ArtIdForItemId(int? itemId)
		{
			if ((object)itemId == null)
			{
				return null;
			}

			IDbConnection conn = null;
			IDataReader reader = null;

			int? artId = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT art_id FROM art_item WHERE item_id = @itemid", conn);
				q.AddNamedParam("@itemid", itemId);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					// Grab the first available item id that is associated with this art id
					// doesn't matter which one because they all have the same art
					artId = reader.GetInt32(0);
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

			return artId;
		}

		public static int? ArtIdForMd5(string hash)
		{
			if ((object)hash == null)
			{
				return null;
			}

			IDbConnection conn = null;
			IDataReader reader = null;

			int? artId = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT art_id FROM art WHERE md5_hash = @md5hash", conn);
				q.AddNamedParam("@md5hash", hash);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					artId = reader.GetInt32(0);
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

			return artId;
		}

		public static bool UpdateArtItemRelationship(int? artId, int? itemId, bool replace)
		{
			if (artId == null || itemId == null)
			{
				return false;
			}

			bool success = false;
			IDbConnection conn = null;

			try
			{
				// insert the song into the database
				conn = Database.GetDbConnection();
				string command = replace ? "REPLACE" : "INSERT OR IGNORE";
				IDbCommand q = Database.GetDbCommand(command + " INTO art_item (art_id, item_id) " + 
													 "VALUES (@artid, @itemid)", conn);

				q.AddNamedParam("@artid", artId);
				q.AddNamedParam("@itemid", itemId);
				q.Prepare();

				if (q.ExecuteNonQueryLogged() > 0)
				{
					success = true;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, null);
			}

			return success;
		}

		public static bool RemoveArtRelationshipForItemId(int? itemId)
		{
			if ((object)itemId == null)
			{
				return false;
			}

			bool success = false;
			IDbConnection conn = null;

			try
			{
				// insert the song into the database
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("DELETE FROM art_item WHERE item_id = @itemid" , conn);

				q.AddNamedParam("@itemid", itemId);
				q.Prepare();

				if (q.ExecuteNonQueryLogged() > 0)
				{
					success = true;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, null);
			}

			return success;
		}

		public static bool UpdateItemsToNewArtId(int? oldArtId, int? newArtId)
		{
			if ((object)oldArtId == null || (object)newArtId == null)
			{
				return false;
			}

			bool success = false;
			IDbConnection conn = null;

			try
			{
				// insert the song into the database
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("UPDATE art_item SET art_id = @newartid WHERE art_id = @oldartid", conn);

				q.AddNamedParam("@newartid", newArtId);
				q.AddNamedParam("@oldartid", oldArtId);
				q.Prepare();

				if (q.ExecuteNonQueryLogged() > 0)
				{
					success = true;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, null);
			}

			return success;
		}
	}
}
