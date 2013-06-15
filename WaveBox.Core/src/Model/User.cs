using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WaveBox.Static;
using WaveBox.Model;
using System.Security.Cryptography;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Model
{
	public class User
	{
		// PBKDF2 iterations
		public const int HashIterations = 2500;

		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonProperty("userId")]
		public int? UserId { get; set; }

		[JsonProperty("userName")]
		public string UserName { get; set; }

		// This is only used after test account creation
		[JsonProperty("password"), IgnoreRead, IgnoreWrite]
		public string Password { get; set; }

		[JsonProperty("sessions"), IgnoreRead, IgnoreWrite]
		public List<Session> Sessions { get; set; }

		[JsonIgnore]
		public string PasswordHash { get; set; }

		[JsonIgnore]
		public string PasswordSalt { get; set; }

		[JsonIgnore, IgnoreRead, IgnoreWrite]
		public string SessionId { get; set; }

		[JsonProperty("lastfmSession")]
		public string LastfmSession { get; set; }

		[JsonProperty("createTime")]
		public long? CreateTime { get; set; }

		[JsonProperty("deleteTime")]
		public long? DeleteTime { get; set; }
	
		public User()
		{

		}

		public static string UserNameForSessionid(string sessionId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.ExecuteScalar<string>("SELECT User.UserName FROM Session JOIN User USING (UserId) WHERE SessionId = ?", sessionId);
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

		public bool UpdateSession(string sessionId)
		{
			// Update user's session based on its session ID
			Session s = new Session.Factory().CreateSession(sessionId);

			if (s != null)
			{
				return s.UpdateSession();
			}

			return false;
		}

		public List<Session> ListOfSessions()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<Session>("SELECT RowId, * FROM Session WHERE UserId = ?", UserId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return new List<Session>();
		}

		public static List<User> AllUsers()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<User>("SELECT * FROM User ORDER BY UserName");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return new List<User>();
		}

		public static List<User> ExpiredUsers()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<User>("SELECT * FROM User WHERE DeleteTime <= ? ORDER BY UserName", DateTime.Now.ToUniversalUnixTimestamp());
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return new List<User>();
		}

		public static int CompareUsersByName(User x, User y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.UserName, y.UserName);
		}

		// Compute password hash using PBKDF2
		private static string ComputePasswordHash(string password, string salt)
		{
			// Hash using predefined iterations
			return ComputePasswordHash(password, salt, HashIterations);
		}

		// Compute password hash using PBKDF2
		private static string ComputePasswordHash(string password, string salt, int iterations)
		{
			// Convert salt to byte array
			byte[] saltBytes = Encoding.UTF8.GetBytes(salt);

			// Hash using PBKDF2 with salt and predefined iterations
			using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations))
			{
				var key = pbkdf2.GetBytes(64);
				return Convert.ToBase64String(key);
			}
		}

		// Use RNG crypto service to generate random bytes for salt
		private static string GeneratePasswordSalt()
		{
			// Create byte array to store salt
			byte[] salt = new byte[32];

			// Fill array using RNG
			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(salt);
			}

			// Return string representation
			return Convert.ToBase64String(salt);
		}

		public void UpdatePassword(string password)
		{
			string salt = GeneratePasswordSalt();
			string hash = ComputePasswordHash(password, salt);

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.Execute("UPDATE User SET PasswordHash = ?, PasswordSalt = ? WHERE UserName = ?", hash, salt, UserName);

				if (affected > 0)
				{
					PasswordHash = hash;
					PasswordSalt = salt;
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

		public void UpdateLastfmSession(string sessionKey)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.Execute("UPDATE User SET LastfmSession = ? WHERE UserName = ?", sessionKey, UserName);

				if (affected > 0)
				{
					LastfmSession = sessionKey;
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

		// Verify password, using timing attack resistant approach
		// Credit to PHP5.5 Password API for this method
		public bool Authenticate(string password)
		{
			// Compute hash
			string hash = ComputePasswordHash(password, PasswordSalt);

			// Ensure hashes are same length
			if (hash.Length != PasswordHash.Length)
			{
				return false;
			}

			// Compare ASCII value of each character, bitwise OR any diff
			int status = 0;
			for (int i = 0; i < hash.Length; i++)
			{
				status |= ((int)hash[i] ^ (int)PasswordHash[i]);
			}

			return status == 0;
		}

		public bool CreateSession(string password, string clientName)
		{
			// On successful authentication, create session!
			if (Authenticate(password))
			{
				Session s = Session.CreateSession(Convert.ToInt32(UserId), clientName);
				if (s != null)
				{
					SessionId = s.SessionId;
					return true;
				}
			}

			return false;
		}

		public void Delete()
		{
			if (ReferenceEquals(UserId, null))
				return;

			// Delete the user
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.Execute("DELETE FROM User WHERE UserId = ?", UserId);

				if (affected > 0)
				{
					// Delete associated sessions
					Session.DeleteSessionsForUserId((int)UserId);
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

		public class Factory
		{
			public User CreateUser(int userId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					var result = conn.DeferredQuery<User>("SELECT * FROM User WHERE UserId = ?", userId);

					foreach (var u in result)
					{
						return u;
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

				var user = new User();
				user.UserId = userId;
				return user;
			}

			public User CreateUser(string userName)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					var result = conn.DeferredQuery<User>("SELECT * FROM User WHERE UserName = ?", userName);

					foreach (var u in result)
					{
						return u;
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

				var user = new User();
				user.UserName = userName;
				return user;
			}

			public User CreateUser(string userName, string password, long? deleteTime)
			{
				int? itemId = Item.GenerateItemId(ItemType.User);
				if (itemId == null)
				{
					return null;
				}

				string salt = GeneratePasswordSalt();
				string hash = ComputePasswordHash(password, salt);

				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					var u = new User();
					u.UserId = itemId;
					u.UserName = userName;
					u.PasswordHash = hash;
					u.Password = password;
					u.PasswordSalt = salt;
					u.CreateTime = DateTime.Now.ToUniversalUnixTimestamp();
					u.DeleteTime = deleteTime;
					conn.Insert(u);

					return u;
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					conn.Close();
				}

				return new User();
			}

			public User CreateTestUser(long? durationSeconds)
			{
				// Create a new user with random username and password, that lasts for the specified duration
				if (ReferenceEquals(durationSeconds, null))
				{
					// If no duration specified, use 24 hours
					durationSeconds = 60 * 60 * 24;
				}

				return CreateUser(Utility.RandomString(16), Utility.RandomString(16), DateTime.Now.ToUniversalUnixTimestamp() + durationSeconds);
			}
		}
	}
}
