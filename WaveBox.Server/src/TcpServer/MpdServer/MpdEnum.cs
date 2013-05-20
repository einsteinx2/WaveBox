using System;
using System.Collections.Generic;
using System.Text;

namespace WaveBox.TcpServer.Mpd
{
	public static class MpdCommand
	{
		public const string Audio = "audio";
		public const string Bitrate = "bitrate";
		public const string Close = "close";
		public const string CommandListBegin = "command_list_begin";
		public const string CommandListEnd = "command_list_end";
		public const string CommandListOkBegin = "command_list_ok_begin";
		public const string Consume = "consume";
		public const string Crossfade = "xfade";
		public const string Error = "error";
		public const string ListPlaylist = "listplaylist";
		public const string LsInfo = "lsinfo";
		public const string MixrampDb = "mixrampdb";
		public const string MixrampDelay = "mixrampdelay";
		public const string Next = "next";
		public const string Pause = "pause";
		public const string Ping = "ping";
		public const string Play = "play";
		public const string Playlist = "playlist";
		public const string PlaylistLength = "playlistlength";
		public const string Previous = "previous";
		public const string Random = "random";
		public const string Repeat = "repeat";
		public const string Single = "single";
		public const string Song = "song";
		public const string SongId = "songid";
		public const string State = "state";
		public const string Status = "status";
		public const string Stop = "stop";
		public const string Time = "time";
		public const string UpdatingDb = "updatingdb";
	}

	public static class MpdResponse
	{
		public const string Greeting = "OK MPD 0.13.0";
		public const string Ok = "OK";
		public const string ListOk = "list_OK";
		public const string Error = "ACK";
	}

	public enum MpdError
	{
		None = 0,
		NotList = 1,
		Arg = 2,
		Password = 3,
		Permission = 4,
		Unknown = 5,
		NoExist = 50,
		PlaylistMax = 51,
		System = 52,
		PlaylistLoad = 53,
		UpdateAlready = 54,
		PlayerSync = 55,
		Exist = 56
	}
}
