using System;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Core.Injected
{
	public interface IDatabase
	{
		string DatabaseTemplatePath();
		string DatabasePath();

		string QuerylogTemplatePath();
		string QuerylogPath();

		object DbBackupLock();

		void DatabaseSetup();

		ISQLiteConnection GetSqliteConnection();

		ISQLiteConnection GetQueryLogSqliteConnection();

		long LastQueryLogId();
	}
}

