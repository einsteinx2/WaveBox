using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Core.Extensions
{
	public static class IDatabaseExtensions
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Note: this extension class exists so that we may provide more concise methods for accessing objects
		// using simple database queries, without having to complicate the interface.

		// Retrieve a single object of type T using query string and prepared arguments
		public static T GetSingle<T>(this IDatabase database, string query, params object[] args) where T : new()
		{
			ISQLiteConnection conn = null;
			try
			{
				// Get database connection, use query to generate object of type T
				conn = database.GetSqliteConnection();
				var result = conn.DeferredQuery<T>(query, args);

				// If result found, return it
				foreach (T obj in result)
				{
					return obj;
				}
			}
			catch (Exception e)
			{
				logger.Error("query: " + query);
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			// If no result, return blank instance
			return default (T);
		}
	}
}
