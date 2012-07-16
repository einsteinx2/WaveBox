using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Data.Sqlite;
using System.Data.SqlTypes;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using System.Security.Cryptography;
using TagLib;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class CoverArt
	{
		public const string ART_PATH = "art";
		public const string TMP_ART_PATH = "art/tmp";

		/// <summary>
		/// Properties
		/// </summary>

		[JsonProperty("artId")]
		public long ArtId { get; set; }

		[JsonProperty("adlerHash")]
		public long AdlerHash { get; set; }

		public string ArtFile()
		{
			string artf = ART_PATH + Path.DirectorySeparatorChar + AdlerHash;
			return artf;
		}

		/// <summary>
		/// Constructors
		/// </summary>

		public CoverArt()
		{
		}

		public CoverArt(long artId)
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT * FROM art WHERE art_id = @artid");

					q.Parameters.AddWithValue("@artid", artId);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						ArtId = reader.GetInt64(0);
						AdlerHash = reader.GetInt64(1);
					}

					reader.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		// used for getting art from a file.
		public CoverArt(FileStream fs)
		{
			// create an array the length of the file
			byte[] data = new byte[fs.Length];

			// read the data in
			fs.Read(data, 0, Convert.ToInt32(fs.Length));

			// compute the hash of the data
			var md5 = new MD5CryptoServiceProvider();
			this.AdlerHash = BitConverter.ToInt64(md5.ComputeHash(data), 0);

			CheckDatabaseAndPerformCopy(data);
		}

		// used for getting art from a tag.
		public CoverArt(FileInfo af)
		{
			var file = TagLib.File.Create(af.FullName);
			if (file.Tag.Pictures.Length > 0)
			{
				var data = file.Tag.Pictures[0].Data.Data;
				var md5 = new MD5CryptoServiceProvider();
				this.AdlerHash = BitConverter.ToInt64(md5.ComputeHash(data), 0);

				CheckDatabaseAndPerformCopy(data);
			}
		}

		private void CheckDatabaseAndPerformCopy(byte[] data)
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT * FROM art WHERE adler_hash = @adlerhash");
					q.Parameters.AddWithValue("@adlerhash", AdlerHash);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						// the art is already in the database
						this.ArtId = reader.GetInt64(reader.GetOrdinal("art_id"));
					}

					// the art is not already in the database
					else
					{
						try
						{
							System.IO.File.WriteAllBytes(ART_PATH + this.AdlerHash, data);
						}
						catch (Exception e)
						{
							Console.WriteLine(e.ToString());
						}
						finally
						{
							Database.Close(conn, reader);
						}

						try
						{
							var q1 = new SqliteCommand("INSERT INTO art (adler_hash) VALUES (@adlerhash)");

							q1.Parameters.AddWithValue("@adlerhash", AdlerHash);

							var conn1 = Database.GetDbConnection();
							q1.Connection = conn1;
							q1.Prepare();
							long result = q1.ExecuteNonQuery();

							if (result < 1)
							{
								Console.WriteLine("Something went wrong with the art insert: ");
							}

							try
							{
								q1.CommandText = "SELECT last_insert_rowid()";
								this.ArtId = Convert.ToInt64((q1.ExecuteScalar()).ToString());
							}
							catch (Exception e)
							{
								Console.WriteLine("\r\n\r\nGetting identity: " + e.ToString() + "\r\n\r\n");
							}
							finally
							{

							}
						}
						catch (SqliteException e)
						{
							Console.WriteLine("\r\n\r\n" + e.Message + "\r\n\r\n");
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				finally
				{

					Database.Close(conn, reader);
				}
			}
		}
	}
}
