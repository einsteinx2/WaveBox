using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using WaveBox;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model
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
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return success;
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
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return success;
		}

		public override string ToString()
		{
			return String.Format("[Session: RowId={0}, SessionId={1}, UserId={2}]", this.RowId, this.SessionId, this.UserId);
		}
	}
}
