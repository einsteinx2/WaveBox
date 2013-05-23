using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox;
using WaveBox.Model;
using WaveBox.Static;
using System.IO;
using TagLib;
using Newtonsoft.Json;
using System.Diagnostics;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Model
{
	public class Session
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonProperty("rowId")]
		public int RowId { get; set; }

		[JsonIgnore]
		public string SessionId { get; set; }

		[JsonProperty("userId")]
		public int? UserId { get; set; }

		[JsonProperty("clientName")]
		public string ClientName { get; set; }

		[JsonProperty("createTime")]
		public long? CreateTime { get; set; }

		[JsonProperty("updateTime")]
		public long? UpdateTime { get; set; }

		public Session()
		{
		}

		public bool UpdateSession()
		{
			bool success = false;

			// Get current UNIX time
			long unixTime = DateTime.Now.ToUniversalUnixTimestamp();

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("UPDATE session SET UpdateTime = ? WHERE SessionId = ?", unixTime, SessionId);

				if (affected > 0)
				{
					UpdateTime = unixTime;
					success = true;
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

			return success;
		}

		public static Session CreateSession(int userId, string clientName)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				var session = new Session();
				session.SessionId = Utility.SHA1(Utility.RandomString(100));
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
				conn.Close();
			}

			return new Session();
		}

		public bool DeleteSession()
		{
			ISQLiteConnection conn = null;
			bool success = false;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM session WHERE ROWID = ?", RowId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return success;
		}

		public static List<Session> AllSessions()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.Query<Session>("SELECT RowId, * FROM session");
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

		public static int CountSessions()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT count(RowId) FROM session");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return 0;
		}

		public static bool DeleteSessionsForUserId(int userId)
		{
			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM session WHERE UserId = ?", userId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
			}

			return success;
		}

		public class Factory
		{
			public Session CreateSession(int rowId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					var result = conn.DeferredQuery<Session>("SELECT RowId, * FROM session WHERE RowId = ?", rowId);

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
					conn.Close();
				}

				return new Session();
			}

			public Session CreateSession(string sessionId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Database.GetSqliteConnection();
					var result = conn.DeferredQuery<Session>("SELECT RowId, * FROM session WHERE SessionId = ?", sessionId);

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
					conn.Close();
				}

				return new Session();
			}
		}
	}
}
