﻿using System;
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
	public class CoverArt
	{
		public readonly string ART_PATH = "art" + Path.DirectorySeparatorChar;
		public readonly string TMP_ART_PATH = "art" + Path.DirectorySeparatorChar + "tmp";

		/// <summary>
		/// Properties
		/// </summary>

		[JsonProperty("artId")]
		public int? ArtId { get; set; }

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

		public CoverArt(int artId)
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
					ArtId = reader.GetInt32(0);
					AdlerHash = reader.GetInt64(1);
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
		}
	}
}
