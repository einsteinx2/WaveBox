using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Collections.Generic;
using WaveBox.Core.Extensions;
using WaveBox.Core.Static;
using System.Linq;

namespace WaveBox.Core.Model.Repository
{
	public class SessionRepository : ISessionRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		private IDictionary<string, Session> Sessions { get; set; }

		public SessionRepository(IDatabase database)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}

			this.database = database;

			this.Sessions = new Dictionary<string, Session>();
			this.ReloadSessions();
		}

		private void ReloadSessions()
		{
			lock (this.Sessions)
			{
				this.Sessions.Clear();

				ISQLiteConnection conn = null;
				try
				{
					conn = database.GetSqliteConnection();

					foreach (Session s in conn.DeferredQuery<Session>("SELECT RowId AS RowId,* FROM Session"))
					{
						this.Sessions[s.SessionId] = s;
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
			}
		}

		public Session SessionForRowId(int rowId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<Session>("SELECT RowId AS RowId,* FROM Session WHERE RowId = ?", rowId);

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
			lock (this.Sessions)
			{
				Session session = null;
				if (this.Sessions.ContainsKey(sessionId))
				{
					return this.Sessions[sessionId];
				}

				return null;
			}
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

				// Set a default client name if not provided
				if (clientName == null)
				{
					clientName = "wavebox";
				}

				session.ClientName = clientName;
				long unixTime = DateTime.Now.ToUniversalUnixTimestamp();
				session.CreateTime = unixTime;
				session.UpdateTime = unixTime;

				int affected = conn.InsertLogged(session);

				// Fetch the row ID just created
				session.RowId = conn.ExecuteScalar<int>("SELECT RowId AS RowId FROM Session WHERE SessionId = ?", session.SessionId);

				if (affected > 0)
				{
					lock (this.Sessions)
					{
						this.Sessions[session.SessionId] = session;
					}

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

		public IList<Session> AllSessions()
		{
			lock (this.Sessions)
			{
				return new List<Session>(this.Sessions.Select(x => x.Value).ToList());
			}
		}

		public int CountSessions()
		{
			lock (this.Sessions)
			{
				return this.Sessions.Count;
			}
		}

		public bool UpdateSessionCache(string sessionId, Session session)
		{
			lock (this.Sessions)
			{
				this.Sessions[sessionId] = session;
			}

			return true;
		}

		// Remove a session by row ID, reload the cached sessions list
		public bool DeleteSessionForRowId(int rowId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM Session WHERE RowId = ?", rowId);

				if (affected > 0)
				{
					this.ReloadSessions();

					return true;
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

			return false;
		}

		public bool DeleteSessionsForUserId(int userId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM Session WHERE UserId = ?", userId);

				if (affected > 0)
				{
					this.ReloadSessions();

					return true;
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

			return false;
		}

		public int? UserIdForSessionid(string sessionId)
		{
			if (sessionId == null)
			{
				return null;
			}

			lock (this.Sessions)
			{
				if (this.Sessions.ContainsKey(sessionId))
				{
					return this.Sessions[sessionId].UserId;
				}

				return null;
			}
		}
	}
}
