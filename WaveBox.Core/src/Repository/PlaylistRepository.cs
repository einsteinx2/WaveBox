using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using System.Collections.Generic;
using WaveBox.Core.Extensions;

namespace WaveBox.Core.Model.Repository
{
	public class PlaylistRepository : IPlaylistRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public PlaylistRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public Playlist PlaylistForId(int playlistId)
		{
			return this.database.GetSingle<Playlist>("SELECT * FROM Playlist WHERE PlaylistId = ?", playlistId);
		}

		public Playlist PlaylistForName(string playlistName)
		{
			return this.database.GetSingle<Playlist>("SELECT * FROM Playlist WHERE PlaylistName = ?", playlistName);
		}

		public IList<Playlist> AllPlaylists()
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				return conn.Query<Playlist>("SELECT * FROM Playlist");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return new List<Playlist>();
		}
	}
}

