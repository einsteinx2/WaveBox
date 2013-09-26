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

		private IList<Session> Sessions { get; set; }

		public SessionRepository(IDatabase database)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}

			this.database = database;

			this.Sessions = new List<Session>();
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
					this.Sessions.AddRange(conn.DeferredQuery<Session>("SELECT RowId AS RowId,* FROM Session"));
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
				Session session = Sessions.SingleOrDefault(s => s.SessionId == sessionId);
				if (session == null)
				{
					session = new Session();
				}

				return session;
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

				if (affected > 0)
				{
					lock (Sessions)
					{
						Sessions.Add(session);
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
				return new List<Session>(this.Sessions);
			}
		}

		public int CountSessions()
		{
			lock (this.Sessions)
			{
				return this.Sessions.Count;
			}
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

				if (success)
				{
					this.ReloadSessions();
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

			return success;
		}

		public int? UserIdForSessionid(string sessionId)
		{
			lock (this.Sessions)
			{
				Session session = Sessions.SingleOrDefault(s => s.SessionId == sessionId);
				if (session != null)
				{
					return session.UserId;
				}

				return null;
			}
		}
	}
}

