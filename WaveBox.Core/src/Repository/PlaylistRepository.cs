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
			{
				throw new ArgumentNullException("database");
			}

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
			return this.database.GetList<Playlist>("SELECT * FROM Playlist");
		}
	}
}
