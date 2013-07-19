using System;
using WaveBox.Core.Injection;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;

namespace WaveBox.Model.Repository
{
	public class UserRepository : IUserRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public UserRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public string UserNameForSessionid(string sessionId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.ExecuteScalar<string>("SELECT User.UserName FROM Session JOIN User USING (UserId) WHERE SessionId = ?", sessionId);
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

		public List<User> AllUsers()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				List<User> users = conn.Query<User>("SELECT * FROM User ORDER BY UserName");

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

		public List<User> ExpiredUsers()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<User>("SELECT * FROM User WHERE DeleteTime <= ? ORDER BY UserName", DateTime.Now.ToUniversalUnixTimestamp());
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

