using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WaveBox.Static;
using WaveBox.Model;
using System.Security.Cryptography;
using TagLib;
using Newtonsoft.Json;
using Cirrious.MvvmCross.Plugins.Sqlite;

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

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public Stream Stream { get { return CreateStream(); } }

		/// <summary>
		/// Constructors
		/// </summary>

		public Art()
		{

		}

		public void InsertArt()
		{
			int? itemId = Item.GenerateItemId(ItemType.Art);
			if (itemId == null)
			{
				return;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				ArtId = itemId;
				int affected = conn.InsertLogged(this);

				if (affected == 0)
				{
					ArtId = null;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
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
			Song song = new Song.Factory().CreateSong(songId);
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

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				string artId = conn.ExecuteScalar<string>("SELECT ArtId FROM art WHERE LastModified = ? AND FilePath = ?", filePath, lastModified);

				if (ReferenceEquals(artId, null))
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
				conn.Close();
			}

			return needsUpdating;
		}

		public static int? ItemIdForArtId(int? artId)
		{
			if ((object)artId == null)
			{
				return null;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int itemId = conn.ExecuteScalar<int>("SELECT ItemId FROM ArtItem WHERE ArtId = ?", artId);
				return itemId == 0 ? (int?)null : itemId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return null;
		}

		public static int? ArtIdForItemId(int? itemId)
		{
			if ((object)itemId == null)
			{
				return null;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int artId = conn.ExecuteScalar<int>("SELECT ArtId FROM ArtItem WHERE ItemId = ?", itemId);
				return artId == 0 ? (int?)null : artId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return null;
		}

		public static int? ArtIdForMd5(string hash)
		{
			if ((object)hash == null)
			{
				return null;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int artId = conn.ExecuteScalar<int>("SELECT ArtId FROM art WHERE Md5Hash = ?", hash);
				return artId == 0 ? (int?)null : artId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return null;
		}

		public static bool UpdateArtItemRelationship(int? artId, int? itemId, bool replace)
		{
			if (artId == null || itemId == null)
			{
				return false;
			}

			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				// insert the song into the database
				conn = Database.GetSqliteConnection();
				string type = replace ? "REPLACE" : "INSERT OR IGNORE";
				int affected = conn.ExecuteLogged(type + " INTO ArtItem (ArtId, ItemId) VALUES (?, ?)", artId, itemId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
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
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM ArtItem WHERE ItemId = ?", itemId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
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
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("UPDATE ArtItem SET ArtId = ? WHERE ArtId = ?", newArtId, oldArtId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return success;
		}

		public class Factory
		{
			public Art CreateArt(int artId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					var result = conn.DeferredQuery<Art>("SELECT * FROM art WHERE ArtId = ?", artId);

					foreach (Art a in result)
					{
						return a;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					conn.Close();
				}

				return new Art();
			}

			// used for getting art from a file.
			public Art CreateArt(string filePath)
			{
				FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

				// compute the hash of the file stream
				Art art = new Art();
				art.Md5Hash = CalcMd5Hash(fs);
				art.FileSize = fs.Length;
				art.LastModified = System.IO.File.GetLastWriteTime(fs.Name).ToUniversalUnixTimestamp();
				art.ArtId = Art.ArtIdForMd5(art.Md5Hash);
				art.FilePath = filePath;

				if ((object)art.ArtId == null)
				{
					art.InsertArt();
				}

				return art;
			}

			// used for getting art from a tag.
			// We don't set the FilePath here, because that is only used for actual art files on disk
			public Art CreateArt(TagLib.File file)
			{
				Art art = new Art();

				if (file.Tag.Pictures.Length > 0)
				{
					byte[] data = file.Tag.Pictures[0].Data.Data;
					art.Md5Hash = CalcMd5Hash(data);
					art.FileSize = data.Length;
					art.LastModified = System.IO.File.GetLastWriteTime(file.Name).ToUniversalUnixTimestamp();

					art.ArtId = Art.ArtIdForMd5(art.Md5Hash);
					if (art.ArtId == null)
					{
						// This art isn't in the database yet, so add it
						art.InsertArt();
					}
				}

				return art;
			}
		}
	}
}
