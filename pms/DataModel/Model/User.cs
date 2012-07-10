using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;
using MediaFerry.DataModel.Singletons;
using MediaFerry.DataModel.Model;
using System.Security.Cryptography;

namespace MediaFerry.DataModel.Model
{
	class User
	{
		private int _userId;
		public int UserId
		{
			get
			{
				return _userId;
			}
			set
			{
				_userId = value;
			}
		}

		private string _userName;
		public string UserName
		{
			get
			{
				return _userName;
			}
			set
			{
				_userName = value;
			}
		}

		private string _passwordHash;
		public string PasswordHash
		{
			get
			{
				return _passwordHash;
			}
			set
			{
				_passwordHash = value;
			}
		}

		private string _passwordSalt;
		public string PasswordSalt
		{
			get
			{
				return _passwordSalt;
			}
			set
			{
				_passwordSalt = value;
			}
		}

		public User()
		{
		}

		public User(int userId)
		{
			UserId = userId;

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				var q = new SqlCeCommand("SELECT * FROM user WHERE user_id = @userid");
				q.Connection = conn;
				q.Parameters.AddWithValue("@userid", UserId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_setPropertiesFromQueryResult(reader);
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
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
				Database.close(conn, reader);
			}
		}

		public User(string userName)
		{
			UserName = userName;

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				var q = new SqlCeCommand("SELECT * FROM user WHERE user_name = @username");
				q.Connection = conn;
				q.Parameters.AddWithValue("@username", UserName);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_setPropertiesFromQueryResult(reader);
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
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
				Database.close(conn, reader);
			}
		}

		private void _setPropertiesFromQueryResult(SqlCeDataReader reader)
		{
			UserId = reader.GetInt32(reader.GetOrdinal("user_id"));
			UserName = reader.GetString(reader.GetOrdinal("user_name"));
			PasswordHash = reader.GetString(reader.GetOrdinal("password_hash"));
			PasswordSalt = reader.GetString(reader.GetOrdinal("password_salt"));
		}

		private static string _sha1(string sumthis)
		{
			if (sumthis == "" || sumthis == null)
			{
				return "";
			}

			var provider = new SHA1CryptoServiceProvider();
			return BitConverter.ToString(provider.ComputeHash(Encoding.ASCII.GetBytes(sumthis)));
		}

		private static string _computePasswordHash(string password, string salt)
		{
			long startTime = DateTime.Now.Ticks;
			var hash = _sha1(password + salt);

			for (int i = 0; i < 50; i++)
			{
				hash = _sha1(hash + salt);
			}

			return hash;
		}

		private static string _generatePasswordSalt()
		{
			return _sha1(Convert.ToString(DateTime.Now.Ticks));
		}

		public void updatePassword(string password)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			var salt = _generatePasswordSalt();
			var hash = _computePasswordHash(password, salt);

			try
			{
				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				var q = new SqlCeCommand("UPDATE user SET password_hash = @hash, password_salt = @salt WHERE user_name = @username");
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
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}
		}

		public static User createUser(string userName, string password)
		{
			User user = null;

			var salt = _generatePasswordSalt();
			var hash = _computePasswordHash(password, salt);

			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				Database.dbLock.WaitOne();
				conn = Database.getDbConnection();
				var q = new SqlCeCommand("INSERT INTO user (user_name, user_password, user_salt) VALUES (@username, @userhash, @usersalt)");
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
				Database.dbLock.ReleaseMutex();
				Database.close(conn, reader);
			}

			return new User(userName);
		}
	}
}
