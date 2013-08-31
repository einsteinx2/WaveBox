using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IPlaylistRepository
	{
		Playlist PlaylistForId(int playlistId);
		Playlist PlaylistForName(string playlistName);
		IList<Playlist> AllPlaylists();
	}
}
