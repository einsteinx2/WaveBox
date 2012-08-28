using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlTypes;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using System.Security.Cryptography;
using TagLib;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class Art
	{
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
					ArtId = reader.GetInt32(reader.GetOrdinal("art_id"));
					Md5Hash = reader.GetString(reader.GetOrdinal("md5_hash"));
					LastModified = reader.GetInt64(reader.GetOrdinal("art_last_modified"));
					FileSize = reader.GetInt64(reader.GetOrdinal("art_file_size"));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[COVERART(1)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		// used for getting art from a file.
		public Art(FileStream fs)
		{
			// compute the hash of the file stream
			Md5Hash = CalcMd5Hash(fs);
			FileSize = fs.Length;
			LastModified = Convert.ToInt64(System.IO.File.GetLastWriteTime(fs.Name).Ticks);
			ArtId = ArtIdFromMd5(Md5Hash);
		}

		// used for getting art from a tag.
		public Art(TagLib.File file)
		{
			if (file.Tag.Pictures.Length > 0)
			{
				byte[] data = file.Tag.Pictures[0].Data.Data;
				Md5Hash = CalcMd5Hash(data);
				FileSize = data.Length;
				LastModified = Convert.ToInt64(System.IO.File.GetLastWriteTime(file.Name).Ticks);

				ArtId = ArtIdFromMd5(Md5Hash);
				if (ArtId == null)
				{
					// This art isn't in the database yet, so add it
					InsertArt();
				}
			}
		}

		static int? ArtIdFromMd5(string hash)
		{
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
				Console.WriteLine("[COVERART(1)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return artId;
		}

		// Based off of example at http://msdn.microsoft.com/en-us/library/s02tk69a.aspx
		static string CalcMd5Hash(byte[] input)
		{
			using(MD5 md5 = MD5.Create())
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
			using(MD5 md5 = MD5.Create())
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

		public void InsertArt()
		{			
			int? itemId = Database.GenerateItemId(ItemType.Art);
			if (itemId == null)
				return;

			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				// insert the song into the database
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO art (art_id, md5_hash, art_last_modified, art_file_size)" + 
													 "VALUES (@artid, @md5hash, @lastmodified, @filesize)"
													, conn);

				q.AddNamedParam("@artid", itemId);
				q.AddNamedParam("@md5hash", Md5Hash);
				q.AddNamedParam("@lastmodified", LastModified);
				q.AddNamedParam("@filesize", FileSize);
				q.Prepare();

				if (q.ExecuteNonQuery() > 0)
				{
					ArtId = itemId;
				}

				return;
			}
			catch (Exception e)
			{
				Console.WriteLine("[SONG(3)] " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		/*public bool NeedsUpdatingQuick()
		{
			if (ArtId == null)
				return true;

			bool needsUpdating = true;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT art_id FROM art WHERE art_id = @art_id AND md5_hash = @md5hash", conn);

                q.AddNamedParam("@folderid", folderId);
				q.AddNamedParam("@filename", fileName);
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
				Console.WriteLine("[MEDIAITEM(1)] " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return needsUpdating;
		}

		public bool NeedsUpdatingHash()
		{
			if (ArtId == null)
				return true;

			bool needsUpdating = true;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT art_id FROM art WHERE art_id = @art_id AND md5_hash = @md5hash", conn);

                q.AddNamedParam("@folderid", folderId);
				q.AddNamedParam("@filename", fileName);
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
				Console.WriteLine("[MEDIAITEM(1)] " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return needsUpdating;
		}*/



		/*private void CheckDatabaseAndPerformCopy(byte[] data)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM art WHERE adler_hash = @adlerhash", conn);
				q.AddNamedParam("@adlerhash", AdlerHash);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					// the art is already in the database
					this.ArtId = reader.GetInt32(reader.GetOrdinal("art_id"));
				}
				else
				{
					// the art is not already in the database
					try
					{
						System.IO.File.WriteAllBytes(ART_PATH + this.AdlerHash, data);
					}
					catch (Exception e)
					{
						Console.WriteLine("[COVERART(2)] ERROR: " + e.ToString());
					}

					try
					{
						IDbConnection conn1 = Database.GetDbConnection();
						IDbCommand q1 = Database.GetDbCommand("INSERT INTO art (adler_hash) VALUES (@adlerhash)", conn1);

						q1.AddNamedParam("@adlerhash", AdlerHash);

						q1.Prepare();
						int result = q1.ExecuteNonQuery();

						if (result < 1)
						{
							Console.WriteLine("Something went wrong with the art insert: ");
						}

						try
						{
							q1.CommandText = "SELECT last_insert_rowid()";
							this.ArtId = Convert.ToInt32((q1.ExecuteScalar()).ToString());
						}
						catch (Exception e)
						{
							Console.WriteLine("[COVERART(3)]");
							Console.WriteLine("\r\n\r\nGetting identity: " + e.ToString() + "\r\n\r\n");
						}
						finally
						{
							Database.Close(conn1, null);
						}
					}
					catch (Exception e)
					{
						Console.WriteLine("[COVERART(4)]");
						Console.WriteLine("\r\n\r\n" + e.Message + "\r\n\r\n");
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[COVERART(5)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}*/
	}
}
