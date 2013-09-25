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

		private IList<User> Users { get; set; }

		public UserRepository(IDatabase database, IItemRepository itemRepository)
		{
			if (database == null)
				throw new ArgumentNullException("database");
			if (itemRepository == null)
				throw new ArgumentNullException("itemRepository");

			this.database = database;
			this.itemRepository = itemRepository;

			// Load users from the DB into memory for quicker checking
			Users = new List<User>();
			ReloadUsers();
		}

		private void ReloadUsers()
		{
			lock (Users)
			{
				Users.Clear();

				ISQLiteConnection conn = null;
				try
				{
					conn = database.GetSqliteConnection();
					Users.AddRange(conn.DeferredQuery<User>("SELECT * FROM User"));
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
		}

		public User UserForId(int userId)
		{
			lock (Users)
			{
				User user = Users.SingleOrDefault(u => u.UserId == userId);
				if (user == null)
					user = new User() { UserId = userId };

				return user;
			}
		}

		public User UserForName(string userName)
		{
			lock (Users)
			{
				User user = Users.SingleOrDefault(u => u.UserName == userName);
				if (user == null)
					user = new User() { UserName = userName };

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

				// Add to the memory cache
				lock (Users)
				{
					Users.Add(u);
				}

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

		public IList<User> AllUsers()
		{
			lock (Users)
			{
				IList<User> tempUsers =  new List<User>(Users);
				foreach (User u in tempUsers)
				{
					u.Sessions = u.ListOfSessions();
				}
				return tempUsers;
			}
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

