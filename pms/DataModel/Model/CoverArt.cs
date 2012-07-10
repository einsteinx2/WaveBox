using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlServerCe;
using System.Data.SqlTypes;
using MediaFerry.DataModel.Singletons;
using MediaFerry.DataModel.Model;
using System.Security.Cryptography;
using TagLib;
using Newtonsoft.Json;

namespace MediaFerry.DataModel.Model
{
	public class CoverArt
	{
		public const string ART_PATH = "art/";
		public const string TMP_ART_PATH = "art/tmp/";

		/// <summary>
		/// Properties
		/// </summary>

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

		private long _adlerHash;
		[JsonProperty("adlerHash")]
		public long AdlerHash
		{
			get
			{
				return _adlerHash;
			}

			set
			{
				_adlerHash = value;
			}
		}

		public string artFile()
		{
			return ART_PATH + Path.DirectorySeparatorChar + AdlerHash;
		}

		/// <summary>
		/// Constructors
		/// </summary>

		public CoverArt()
		{
		}

		public CoverArt(int artId)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				var q = new SqlCeCommand("SELECT * FROM art WHERE art_id = @artid");

				q.Parameters.AddWithValue("@artid", ArtId);

				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_artId = reader.GetInt32(0);
					_adlerHash = reader.GetInt64(1);
				}

				reader.Close();
				Database.dbLock.ReleaseMutex();
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

		public CoverArt(FileInfo af)
		{
			var file = TagLib.File.Create(af.FullName);
			if (file.Tag.Pictures.Length > 0)
			{
				var data = file.Tag.Pictures[0].Data.Data;
				var md5 = new MD5CryptoServiceProvider();
				_adlerHash = BitConverter.ToInt64(md5.ComputeHash(data), 0);

				SqlCeConnection conn = null;
				SqlCeDataReader reader = null;

				try
				{
					var q = new SqlCeCommand("SELECT * FROM art WHERE adler_hash = @adlerhash");
					q.Parameters.AddWithValue("@adlerhash", AdlerHash);

					Database.dbLock.WaitOne();
					conn = Database.getDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						// the art is already in the database
						_artId = reader.GetInt32(reader.GetOrdinal("art_id"));
						try
						{
							Database.dbLock.ReleaseMutex();
						}

						catch (Exception e)
						{
							Console.WriteLine(e.ToString());
						}
					}

					// the art is not already in the database
					else
					{
						try
						{
							Database.dbLock.ReleaseMutex();
						}

						catch (Exception e)
						{
							Console.WriteLine(e.ToString());
						}
						try
						{
							var writer = new StreamWriter(ART_PATH + _adlerHash);
							writer.Write(file.Tag.Pictures[0].Data.Data);
							writer.Close();
						}

						catch (Exception e)
						{
							Console.WriteLine(e.ToString());
						}

						finally
						{
							Database.close(conn, reader);
						}

						try
						{
							var q1 = new SqlCeCommand("INSERT INTO art (adler_hash) VALUES (@adlerhash)");

							q1.Parameters.AddWithValue("@adlerhash", AdlerHash);

							Database.dbLock.WaitOne();
							var conn1 = Database.getDbConnection();
							q1.Connection = conn1;
							q1.Prepare();
							int result = q1.ExecuteNonQuery();

							if (result < 1)
							{
								Console.WriteLine("Something went wrong with the art insert: ");
							}

							try
							{
								q1.CommandText = "SELECT @@IDENTITY";
								_artId = Convert.ToInt32((q1.ExecuteScalar()).ToString());
							}

							catch (Exception e)
							{
								Console.WriteLine("\r\n\r\nGetting identity: " + e.ToString() + "\r\n\r\n");
							}

							finally
							{
								try
								{
									Database.dbLock.ReleaseMutex();
								}

								catch (Exception e)
								{
									Console.WriteLine(e.ToString());
								}
							}
						}

						catch (SqlCeException e)
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

					Database.close(conn, reader);
				}
			}
		}
	}
}
