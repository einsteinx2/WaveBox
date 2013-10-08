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
			return new T();
		}

		// Retrieve a list of objects of type T using query string and prepared arguments
		public static IList<T> GetList<T>(this IDatabase database, string query, params object[] args) where T : new()
		{
			ISQLiteConnection conn = null;
			try
			{
				// Get database connection, use query to generate object of type T
				conn = database.GetSqliteConnection();

				// If result found, return it
				return conn.Query<T>(query, args);
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
			return new List<T>();
		}

		// Retrieve a scalar value of type T using query string and prepared arguments
		public static T GetScalar<T>(this IDatabase database, string query, params object[] args) where T : new()
		{
			ISQLiteConnection conn = null;
			try
			{
				// Get database connection, use query to fetch scalar value of type T
				conn = database.GetSqliteConnection();
				return conn.ExecuteScalar<T>(query, args);
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

			// Return default value for scalar (reference: null, value: 0)
			return default(T);
		}

		// Insert or replace an object of type T using ORM, while logging to query log
		public static int InsertObject<T>(this IDatabase database, T obj, InsertType insertType = InsertType.Insert)
		{
			ISQLiteConnection conn = null;
			try
			{
				// Get database connection, insert object and log query
				conn = database.GetSqliteConnection();
				return conn.InsertLogged(obj, insertType);
			}
			catch (Exception e)
			{
				logger.Error("insert failed: " + obj);
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			// Return 0 on exception, no rows affected
			return 0;
		}

		// Execute a standard, logged query on the database
		public static int ExecuteQuery(this IDatabase database, string query, params object[] args)
		{
			return database.InternalExecuteQuery(true, query, args);
		}

		// Execute a non-logged query on the database
		public static int ExecuteQueryNonLogged(this IDatabase database, string query, params object[] args)
		{
			return database.InternalExecuteQuery(false, query, args);
		}

		// Execute a query on the database, with optional logging
		private static int InternalExecuteQuery(this IDatabase database, bool logging, string query, params object[] args)
		{
			ISQLiteConnection conn = null;
			try
			{
				// Get database connection, execute and log query
				conn = database.GetSqliteConnection();

				if (logging)
				{
					return conn.ExecuteLogged(query, args);
				}
				else
				{
					return conn.Execute(query, args);
				}
			}
			catch (Exception e)
			{
				logger.Error("execute failed: " + query);
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			// Return 0 on exception, no rows affected
			return 0;
		}
	}
}
