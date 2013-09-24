using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model.Repository
{
	public class UserCache
	{
		public IDictionary<int, User> UserForIdCache;
		public IDictionary<string, User> UserForNameCache;
		public IDictionary<string, string> UserNameForSessionIdCache;
	}

	public class UserRepository : IUserRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;
		private readonly IItemRepository itemRepository;

		private UserCache userCache;

		public UserRepository(IDatabase database, IItemRepository itemRepository)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}
			if (itemRepository == null)
			{
				throw new ArgumentNullException("itemRepository");
			}

			this.database = database;
			this.itemRepository = itemRepository;

			this.userCache = new UserCache();
			this.userCache.UserForIdCache = new Dictionary<int, User>();
			this.userCache.UserForNameCache = new Dictionary<string, User>();
			this.userCache.UserNameForSessionIdCache = new Dictionary<string, string>();
		}

		public User UserForId(int userId)
		{
			ISQLiteConnection conn = null;
			try
			{
				// Return cached result if available
				if (this.userCache.UserForIdCache.ContainsKey(userId))
				{
					return this.userCache.UserForIdCache[userId];
				}

				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<User>("SELECT * FROM User WHERE UserId = ?", userId);

				foreach (var u in result)
				{
					u.Sessions = u.ListOfSessions();

					// Store User in ID cache
					this.userCache.UserForIdCache[userId] = u;

					return u;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			var user = new User();
			user.UserId = userId;
			return user;
		}

		public User UserForName(string userName)
		{
			ISQLiteConnection conn = null;
			try
			{
				// Return cached result if available
				if (this.userCache.UserForNameCache.ContainsKey(userName))
				{
					return this.userCache.UserForNameCache[userName];
				}

				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<User>("SELECT * FROM User WHERE UserName = ?", userName);

				foreach (var u in result)
				{
					u.Sessions = u.ListOfSessions();

					// Store User in name cache
					this.userCache.UserForNameCache[userName] = u;

					return u;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			var user = new User();
			user.UserName = userName;
			return user;
		}

		public User CreateUser(string userName, string password, Role role, long? deleteTime)
		{
			int? itemId = itemRepository.GenerateItemId(ItemType.User);
			if (itemId == null)
			{
				return null;
			}

			string salt = User.GeneratePasswordSalt();
			string hash = User.ComputePasswordHash(password, salt);

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				var u = new User();
				u.UserId = itemId;
				u.UserName = userName;
				u.Role = role;
				u.PasswordHash = hash;
				u.Password = password;
				u.PasswordSalt = salt;
				u.CreateTime = DateTime.Now.ToUniversalUnixTimestamp();
				u.DeleteTime = deleteTime;
				conn.Insert(u);

				return u;
			}
			catch (NullReferenceException)
			{
				logger.IfInfo("User '" + userName + "' already exists, skipping...");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
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

			return CreateUser(Utility.RandomString(16), Utility.RandomString(16), Role.Test, DateTime.Now.ToUniversalUnixTimestamp() + durationSeconds);
		}

		public string UserNameForSessionId(string sessionId)
		{
			ISQLiteConnection conn = null;
			try
			{
				// Return cached result if available
				if (this.userCache.UserNameForSessionIdCache.ContainsKey(sessionId))
				{
					return this.userCache.UserNameForSessionIdCache[sessionId];
				}

				conn = database.GetSqliteConnection();
				string userName = conn.ExecuteScalar<string>("SELECT User.UserName FROM Session JOIN User USING (UserId) WHERE SessionId = ?", sessionId);

				// Store username in name for session ID cache
				this.userCache.UserNameForSessionIdCache[sessionId] = userName;

				return userName;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return null;
		}

		public IList<User> AllUsers()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				List<User> users = conn.Query<User>("SELECT * FROM User ORDER BY UserName COLLATE NOCASE");

				foreach (User u in users)
				{
					u.Sessions = u.ListOfSessions();
				}

				return users;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return new List<User>();
		}

		public IList<User> ExpiredUsers()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<User>("SELECT * FROM User WHERE DeleteTime <= ? ORDER BY UserName COLLATE NOCASE", DateTime.Now.ToUniversalUnixTimestamp());
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return new List<User>();
		}
	}
}

