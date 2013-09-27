using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;
using WaveBox.Core.Static;
using System.Linq;

namespace WaveBox.Core.Model.Repository
{
	public class UserRepository : IUserRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;
		private readonly IItemRepository itemRepository;

		private IDictionary<int, User> Users { get; set; }

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

			// Load users from the DB into memory for quicker checking
			this.Users = new Dictionary<int, User>();
			this.ReloadUsers();
		}

		private bool ReloadUsers()
		{
			lock (this.Users)
			{
				this.Users.Clear();

				ISQLiteConnection conn = null;
				try
				{
					conn = database.GetSqliteConnection();

					foreach (User u in conn.DeferredQuery<User>("SELECT * FROM User"))
					{
						// Don't cache passwords
						u.Password = null;

						this.Users[(int)u.UserId] = u;
					}

					return true;
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					database.CloseSqliteConnection(conn);
				}
			}

			return false;
		}

		public User UserForId(int userId)
		{
			lock (this.Users)
			{
				if (this.Users.ContainsKey(userId))
				{
					return this.Users[userId];
				}

				return new User() { UserId = userId };
			}
		}

		public User UserForName(string userName)
		{
			lock (this.Users)
			{
				User user = this.Users.Values.SingleOrDefault(u => u.UserName == userName);
				if (user == null)
				{
					user = new User() { UserName = userName };
				}

				return user;
			}
		}

		public User CreateUser(string userName, string password, Role role, long? deleteTime)
		{
			int? itemId = itemRepository.GenerateItemId(ItemType.User);
			if (itemId == null)
			{
				return null;
			}

			// Verify user doesn't exist in cache
			if (this.Users.Values.Any(x => x.UserName == userName))
			{
				return new User();
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
				u.Password = password;
				u.PasswordHash = hash;
				u.PasswordSalt = salt;
				u.CreateTime = DateTime.Now.ToUniversalUnixTimestamp();
				u.DeleteTime = deleteTime;
				conn.Insert(u);

				this.ReloadUsers();

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

			return this.CreateUser(Utility.RandomString(16), Utility.RandomString(16), Role.Test, DateTime.Now.ToUniversalUnixTimestamp() + durationSeconds);
		}

		public IList<User> AllUsers()
		{
			lock (this.Users)
			{
				IList<User> tempUsers = new List<User>(this.Users.Values.ToList());

				foreach (User u in tempUsers)
				{
					u.Sessions = u.ListOfSessions();
				}

				return tempUsers;
			}
		}

		public bool DeleteFromUserCache(User user)
		{
			lock (this.Users)
			{
				this.Users.Remove((int)user.UserId);
			}

			return true;
		}

		public bool UpdateUserCache(User user)
		{
			lock (this.Users)
			{
				this.Users[(int)user.UserId] = user;
			}

			return true;
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
