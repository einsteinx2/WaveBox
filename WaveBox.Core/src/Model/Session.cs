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
using WaveBox.Core.Extensions;
using Ninject;
using WaveBox.Core.Injected;

namespace WaveBox.Model
{
	public class Session
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[JsonProperty("rowId"), IgnoreWrite]
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
				// Not logged, because sessions aren't needed in the backup database anyway
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int affected = conn.Execute("UPDATE Session SET UpdateTime = ? WHERE SessionId = ?", unixTime, SessionId);

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
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
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
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM Session WHERE RowId = ?", RowId);

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
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<Session>("SELECT RowId, * FROM Session");
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
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.ExecuteScalar<int>("SELECT COUNT(RowId) FROM Session");
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
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM Session WHERE UserId = ?", userId);

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
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Session>("SELECT RowId, * FROM Session WHERE RowId = ?", rowId);

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
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Session>("SELECT RowId, * FROM Session WHERE SessionId = ?", sessionId);

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
