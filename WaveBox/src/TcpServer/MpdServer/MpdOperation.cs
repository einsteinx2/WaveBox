using System;
using System.Collections.Generic;
using System.Text;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;

namespace WaveBox.TcpServer.Mpd
{
	public static class MpdOperation
	{
		public static string LsInfo(string arg)
		{
			string buffer = null;

			// Check for null argument, meaning root directory
			Folder f = new Folder(1);

			buffer += "directory: " + f.FolderName + "\n";

			return buffer;
		}

		public static string Status()
		{
			string buffer = null;

			// Convert volume to nearest 0-100 int
			string volume = Math.Round(Jukebox.Instance.Volume() * 100.0f).ToString();
			buffer += "volume: " + volume + "\n";

			// Convert booleans to int
			int s = Jukebox.Instance.Repeat ? 1 : 0;
			buffer += "repeat: " + s + "\n";
			s = Jukebox.Instance.Random ? 1 : 0;
			buffer += "random: " + s + "\n";
			s = Jukebox.Instance.Single ? 1 : 0;
			buffer += "single: " + s + "\n";
			s = Jukebox.Instance.Consume ? 1 : 0;
			buffer += "consume: " + s + "\n";

			//buffer += "playlist: " + Jukebox.Instance.PlaylistVersion + "\n";
			//buffer += "playlistlength: " + Jukebox.Instance.PlaylistLength + "\n";
			//buffer += "xfade: " + Jukebox.Instance.Crossfade + "\n";

			// Convert state to string
			string stateStr = null;
			if (Jukebox.Instance.State == JukeboxState.Pause)
			{
				stateStr = "pause";
			}
			else if (Jukebox.Instance.State == JukeboxState.Play)
			{
				stateStr = "play";
			}
			else
			{
				stateStr = "stop";
			}
			buffer += "state: " + stateStr + "\n";
			return buffer;
		}
	}
}
