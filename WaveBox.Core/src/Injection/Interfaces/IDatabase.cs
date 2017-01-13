using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core {
    public interface IDatabase {
        object DbBackupLock { get; }

        int Version { get; }

        string DatabaseTemplatePath { get; }
        string DatabasePath { get; }

        string QuerylogTemplatePath { get; }
        string QuerylogPath { get; }

        void DatabaseSetup();

        ISQLiteConnection GetSqliteConnection();
        void CloseSqliteConnection(ISQLiteConnection conn);

        ISQLiteConnection GetQueryLogSqliteConnection();
        void CloseQueryLogSqliteConnection(ISQLiteConnection conn);

        long LastQueryLogId { get; }
        IList<QueryLog> QueryLogsSinceId(int queryId);
    }
}

