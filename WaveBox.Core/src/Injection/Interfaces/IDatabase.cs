using System;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Core
{
	public interface IDatabase
	{
		object DbBackupLock { get; }

		string DatabaseTemplatePath();
		string DatabasePath();

		string QuerylogTemplatePath();
		string QuerylogPath();

		void DatabaseSetup();

		ISQLiteConnection GetSqliteConnection();
		void CloseSqliteConnection(ISQLiteConnection conn);

		ISQLiteConnection GetQueryLogSqliteConnection();
		void CloseQueryLogSqliteConnection(ISQLiteConnection conn);

		long LastQueryLogId();
	}
}

