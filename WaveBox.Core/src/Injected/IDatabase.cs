using System;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Core.Injected
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

		ISQLiteConnection GetQueryLogSqliteConnection();

		long LastQueryLogId();
	}
}

