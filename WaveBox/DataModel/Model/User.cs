using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using System.Security.Cryptography;

namespace WaveBox.DataModel.Model
{
	class User
	{
		public int UserId { get; set; }

		public string UserName { get; set; }

		public string PasswordHash { get; set; }

		public string PasswordSalt { get; set; }


		public User()
		{
		}

		public User(int userId)
		{
			UserId = userId;

			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();
					var q = new SQLiteCommand("SELECT * FROM users WHERE user_id = @userid");
					q.Connection = conn;
					q.Parameters.AddWithValue("@userid", UserId);
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						SetPropertiesFromQueryResult(reader);
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

		public User(string userName)
		{
			UserName = userName;

			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();
					var q = new SQLiteCommand("SELECT * FROM users WHERE user_name = @username");
					q.Connection = conn;
					q.Parameters.AddWithValue("@username", userName);
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						SetPropertiesFromQueryResult(reader);
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

		private void SetPropertiesFromQueryResult(SQLiteDataReader reader)
		{
			UserId = reader.GetInt32(reader.GetOrdinal("user_id"));
			UserName = reader.GetString(reader.GetOrdinal("user_name"));
			PasswordHash = reader.GetString(reader.GetOrdinal("user_password"));
			PasswordSalt = reader.GetString(reader.GetOrdinal("user_salt"));
		}

		private static string Sha1(string sumthis)
		{
			if (sumthis == "" || sumthis == null)
			{
				return "";
			}

			var provider = new SHA1CryptoServiceProvider();
			return BitConverter.ToString(provider.ComputeHash(Encoding.ASCII.GetBytes(sumthis)));
		}

		private static string ComputePasswordHash(string password, string salt)
		{
			//long startTime = DateTime.Now.Ticks;
			var hash = Sha1(password + salt);

			for (int i = 0; i < 50; i++)
			{
				hash = Sha1(hash + salt);
			}

			return hash;
		}

		private static string GeneratePasswordSalt()
		{
			return Sha1(Convert.ToString(DateTime.Now.Ticks));
		}

		public void UpdatePassword(string password)
		{
			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;

			var salt = GeneratePasswordSalt();
			var hash = ComputePasswordHash(password, salt);

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();
					var q = new SQLiteCommand("UPDATE users SET user_password = @hash, user_salt = @salt WHERE user_name = @username");
					q.Connection = conn;
					q.Parameters.AddWithValue("@username", UserName);
					q.Prepare();
					q.ExecuteNonQuery();
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

			PasswordHash = hash;
			PasswordSalt = salt;
		}

		public static User CreateUser(string userName, string password)
		{
			var salt = GeneratePasswordSalt();
			var hash = ComputePasswordHash(password, salt);

			SQLiteConnection conn = null;
			SQLiteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();
					var q = new SQLiteCommand("INSERT INTO users (user_name, user_password, user_salt) VALUES (@username, @userhash, @usersalt)");
					q.Connection = conn;
					q.Parameters.AddWithValue("@username", userName);
					q.Parameters.AddWithValue("@userhash", hash);
					q.Parameters.AddWithValue("@usersalt", salt);
					q.Prepare();
					q.ExecuteNonQuery();
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

			return new User(userName);
		}

		public bool Authenticate(string password)
		{
			var hash = ComputePasswordHash(password, PasswordSalt);
			return hash == PasswordHash ? true : false;
		}
	}
}
