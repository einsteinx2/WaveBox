using System;
using WaveBox.Core.Injection;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Model.Repository
{
	public class GenreRepository : IGenreRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public GenreRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public List<Genre> AllGenres()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				return conn.Query<Genre>("SELECT * FROM Genre ORDER BY GenreName");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return new List<Genre>();
		}
	}
}

