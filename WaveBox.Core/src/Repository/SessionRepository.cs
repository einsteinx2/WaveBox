using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Collections.Generic;
using WaveBox.Core.Extensions;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model.Repository
{
	public class SessionRepository : ISessionRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public SessionRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public Session SessionForRowId(int rowId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<Session>("SELECT RowId AS RowId, * FROM Session WHERE RowId = ?", rowId);

				foreach (var session in result)
				{
					return session;
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

			return new Session();
		}

		public Session SessionForSessionId(string sessionId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<Session>("SELECT RowId AS RowId, * FROM Session WHERE SessionId = ?", sessionId);

				foreach (var session in result)
				{
					return session;
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

			return new Session();
		}

		public Session CreateSession(int userId, string clientName)
		{
			ISQLiteConnection conn = null;

			try
			{
				conn = database.GetSqliteConnection();
				var session = new Session();
				session.SessionId = Utility.RandomString(100).SHA1();
				session.UserId = userId;
				session.ClientName = clientName;
				long unixTime = DateTime.Now.ToUniversalUnixTimestamp();
				session.CreateTime = unixTime;
				session.UpdateTime = unixTime;

				int affected = conn.InsertLogged(session);

				if (affected > 0)
				{
					return session;
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

			return new Session();
		}

		public List<Session> AllSessions()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Session>("SELECT RowId AS RowId, * FROM Session");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return new List<Session>();
		}

		public int CountSessions()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(RowId) FROM Session");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return 0;
		}

		public bool DeleteSessionsForUserId(int userId)
		{
			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM Session WHERE UserId = ?", userId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return success;
		}
	}
}

