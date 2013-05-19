using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Newtonsoft.Json;
using WaveBox.Static;
using WaveBox.Model;
using System.Security.Cryptography;

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
		[JsonProperty("password")]
		public string Password { get; set; }

		[JsonProperty("sessions")]
		public List<Session> Sessions { get; set; }

		[JsonIgnore]
		public string PasswordHash { get; set; }

		[JsonIgnore]
		public string PasswordSalt { get; set; }

		[JsonIgnore]
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

		public User(IDataReader reader)
		{
			SetPropertiesFromQueryReader(reader);
		}

		public User(int userId)
		{
			UserId = userId;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM user WHERE user_id = @userid", conn);
				q.AddNamedParam("@userid", UserId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
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

		public User(string userName)
		{
			UserName = userName;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM user WHERE user_name = @username", conn);
				q.AddNamedParam("@username", userName);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryReader(reader);
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

		private void SetPropertiesFromQueryReader(IDataReader reader)
		{
			UserId = reader.GetInt32(reader.GetOrdinal("user_id"));
			UserName = reader.GetString(reader.GetOrdinal("user_name"));
			PasswordHash = reader.GetString(reader.GetOrdinal("user_password"));
			PasswordSalt = reader.GetString(reader.GetOrdinal("user_salt"));
			Sessions = this.ListOfSessions();

			if (reader.GetValue(reader.GetOrdinal("user_lastfm_session")) != DBNull.Value)
			{
				LastfmSession = reader.GetString(reader.GetOrdinal("user_lastfm_session"));
			}
			else
			{
				LastfmSession = null;
			}

			CreateTime = reader.GetInt64(reader.GetOrdinal("create_time"));
			DeleteTime = reader.GetInt64OrNull(reader.GetOrdinal("delete_time"));
		}

		public static string UserNameForSessionid(string sessionId)
		{
			string userName = null;
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT user.user_name FROM session JOIN user USING (user_id) WHERE session_id = @sessionid", conn);
				q.AddNamedParam("@sessionid", sessionId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					userName = reader.GetString(0);
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

			return userName;
		}

		public bool UpdateSession(string sessionId)
		{
			// Update user's session based on its session ID
			Session s = new Session(sessionId);

			if (s != null)
			{
				return s.UpdateSession();
			}

			return false;
		}

		public List<Session> ListOfSessions()
		{
			List<Session> sessions = new List<Session>();
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT ROWID,* FROM session WHERE user_id = @userid", conn);
				q.AddNamedParam("@userid", UserId);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					sessions.Add(new Session(reader));
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

			return sessions;
		}

		public static List<User> AllUsers()
		{
			List<User> users = new List<User>();

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM user", conn);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					users.Add(new User(reader));
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

			users.Sort(User.CompareUsersByName);

			return users;
		}

		public static List<User> ExpiredUsers()
		{
			List<User> users = new List<User>();

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM user WHERE delete_time <= @deletetime", conn);
				q.AddNamedParam("@deletetime", DateTime.Now.ToUniversalUnixTimestamp());
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					users.Add(new User(reader));
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

			users.Sort(User.CompareUsersByName);

			return users;
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
			IDbConnection conn = null;
			IDataReader reader = null;

			string salt = GeneratePasswordSalt();
			string hash = ComputePasswordHash(password, salt);

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("UPDATE user SET user_password = @hash, user_salt = @salt WHERE user_name = @username", conn);
				q.AddNamedParam("@hash", hash);
				q.AddNamedParam("@salt", salt);
				q.AddNamedParam("@username", UserName);
				q.Prepare();
				q.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			PasswordHash = hash;
			PasswordSalt = salt;
		}

		public void UpdateLastfmSession(string sessionKey)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("UPDATE user SET user_lastfm_session = @session WHERE user_name = @username", conn);
				q.AddNamedParam("@session", sessionKey);
				q.AddNamedParam("@username", UserName);
				q.Prepare();
				q.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			LastfmSession = sessionKey;
		}

		public static User CreateUser(string userName, string password, long? deleteTime)
		{
			int? itemId = Item.GenerateItemId(ItemType.User);
			if (itemId == null)
			{
				return null;
			}

			string salt = GeneratePasswordSalt();
			string hash = ComputePasswordHash(password, salt);

			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT OR IGNORE INTO user (user_id, user_name, user_password, user_salt, create_time, delete_time) VALUES (@userid, @username, @userhash, @usersalt, @createtime, @deletetime)", conn);
				q.AddNamedParam("@userid", itemId);
				q.AddNamedParam("@username", userName);
				q.AddNamedParam("@userhash", hash);
				q.AddNamedParam("@usersalt", salt);
				q.AddNamedParam("@createtime", DateTime.Now.ToUniversalUnixTimestamp());
				q.AddNamedParam("@deletetime", deleteTime);
				q.Prepare();

				q.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return new User(userName);
		}

		public static User CreateTestUser(long? durationSeconds)
		{
			// Create a new user with random username and password, that lasts for the specified duration
			if (ReferenceEquals(durationSeconds, null))
			{
				// If no duration specified, use 24 hours
				durationSeconds = 60 * 60 * 24;
			}

			string pass = Utility.RandomString(16);

			User testUser = CreateUser(Utility.RandomString(16), pass, DateTime.Now.ToUniversalUnixTimestamp() + durationSeconds);
			testUser.Password = pass;
			return testUser;
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
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("DELETE FROM user WHERE user_id = @userid", conn);
				q.AddNamedParam("@userid", UserId);
				q.Prepare();

				q.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			// Delete associated sessions
			Session.DeleteSessionsForUserId((int)UserId);
		}
	}
}
