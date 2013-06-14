using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox;
using WaveBox.Model;
using WaveBox.Static;
using System.IO;
using TagLib;
using Newtonsoft.Json;
using System.Diagnostics;

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

		public Session(int rowId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT ROWID,* FROM session WHERE ROWID = @rowid", conn);
				q.AddNamedParam("@rowid", rowId);

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

		public Session(string sessionId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT ROWID,* FROM session WHERE session_id = @sessionid", conn);
				q.AddNamedParam("@sessionid", sessionId);

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

		public Session(IDataReader reader)
		{
			SetPropertiesFromQueryReader(reader);
		}

		private void SetPropertiesFromQueryReader(IDataReader reader)
		{
			try
			{
				RowId = reader.GetInt32(reader.GetOrdinal("ROWID"));
				SessionId = reader.GetStringOrNull(reader.GetOrdinal("session_id"));
				UserId = reader.GetInt32OrNull(reader.GetOrdinal("user_id"));
				ClientName = reader.GetStringOrNull(reader.GetOrdinal("client_name"));
				CreateTime = reader.GetInt32OrNull(reader.GetOrdinal("create_time"));
				UpdateTime = reader.GetInt32OrNull(reader.GetOrdinal("update_time"));
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		public bool UpdateSession()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			bool success = false;

			// Get current UNIX time
			long unixTime = DateTime.Now.ToUniversalUnixTimestamp();

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("UPDATE session SET update_time = @updatetime WHERE session_id = @sessionid", conn);
				q.AddNamedParam("@updatetime", unixTime);
				q.AddNamedParam("@sessionid", SessionId);
				q.Prepare();

				if (q.ExecuteNonQuery() > 0)
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
				Database.Close(conn, reader);
			}

			return success;
		}

		public static Session CreateSession(int userId, string clientName)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			// Generate random string, use SHA1 for session ID
			string sessionId = Utility.RandomString(100).SHA1();

			// Get current UNIX time
			long unixTime = DateTime.Now.ToUniversalUnixTimestamp();

			// Output session
			Session outSession = null;

			// Attempt session creation
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO session (session_id, user_id, client_name, create_time, update_time) VALUES (@sessionid, @userid, @clientname, @createtime, @updatetime)", conn);
				q.AddNamedParam("@sessionid", sessionId);
				q.AddNamedParam("@userid", userId);
				q.AddNamedParam("@clientname", clientName);
				q.AddNamedParam("@createtime", unixTime);
				q.AddNamedParam("@updatetime", unixTime);
				q.Prepare();

				if (q.ExecuteNonQuery() > 0)
				{
					outSession = new Session(sessionId);
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

			return outSession;
		}

		public bool DeleteSession()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			// Attempt session deletion
			bool success = false;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("DELETE FROM session WHERE ROWID = @rowid", conn);
				q.AddNamedParam("@rowid", RowId);
				q.Prepare();

				if (q.ExecuteNonQuery() > 0)
				{
					success = true;
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

			return success;
		}

		public static List<Session> AllSessions()
		{
			List<Session> allSessions = new List<Session>();
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT ROWID,* FROM session", conn);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					allSessions.Add(new Session(reader));
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

			return allSessions;
		}

		public static int? CountSessions()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			int? count = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT count(ROWID) FROM session", conn);
				count = Convert.ToInt32(q.ExecuteScalar());
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return count;
		}

		public static bool DeleteSessionsForUserId(int userId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			// Attempt session deletion
			bool success = false;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("DELETE FROM session WHERE user_id = @userid", conn);
				q.AddNamedParam("@userid", userId);
				q.Prepare();

				if (q.ExecuteNonQuery() > 0)
				{
					success = true;
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

			return success;
		}
	}
}
