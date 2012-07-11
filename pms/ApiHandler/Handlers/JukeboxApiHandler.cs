using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using Bend.Util;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	class JukeboxApiHandler : IApiHandler
	{
		private Jukebox _jukebox;
		private UriWrapper _uri;
		private HttpProcessor _sh;
		//private int _userId;

		public JukeboxApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_jukebox = Jukebox.Instance;
			_uri = uriW;
			_sh = sh;
		}

		public void process()
		{
			int index = 0;
			string s = "";

			string part2 = _uri.getUriPart(2);
			string part3 = _uri.getUriPart(3);

			switch(part2)
			{
				case "play":
					string p3 = null;
					if(_uri.getUriPart(3) != null)
					{
						try
						{
							p3 = _uri.getUriPart(3);
							if(p3 != null)
								index = int.Parse(_uri.getUriPart(3));
						}
						catch (Exception e)
						{
							Console.WriteLine("[JUKEBOXAPI] (play) Error parsing part three of URI: " + e.Message);
						}
					}

					if (p3 == null) _jukebox.Play();
					else _jukebox.PlaySongAtIndex(index);
					break;
				case "pause":
					_jukebox.Pause();
					break;
				case "stop":
					_jukebox.Stop();
					break;
				case "prev":
					_jukebox.Prev();
					break;
				case "next":
					_jukebox.Next();
					break;
				case "status":
					_status();
					break;
				case "playlist":
					_playlist();
					break;
				case "add":
					if(_uri.Parameters.ContainsKey("i"))
					{
						s = "";
						_uri.Parameters.TryGetValue("i", out s);
						_addSongs(s);
					}
					break;
				case "remove":
					if(_uri.Parameters.ContainsKey("i"))
					{
						s = "";
						_uri.Parameters.TryGetValue("i", out s);
						_removeSongs(s);
					}
					break;
				case "move":
					if(_uri.Parameters.ContainsKey("from") && _uri.Parameters.ContainsKey("to"))
					{
						string to, from;
						_uri.Parameters.TryGetValue("from", out from);
						_uri.Parameters.TryGetValue("to", out to);

						if(to != null && from != null)
						{
							_move(to, from);
						}
					}
					break;
				case "clear":
					_jukebox.ClearPlaylist();
					break;
				default: break;

			}
		}

		public void _status()
		{
			PmsHttpServer.sendJson(_sh, JsonConvert.SerializeObject(new JukeboxStatusResponse(_jukebox.IsPlaying, _jukebox.CurrentIndex, _jukebox.Progress())));
		}

		public void _playlist()
		{
			PmsHttpServer.sendJson(_sh, JsonConvert.SerializeObject(new JukeboxPlaylistResponse(_jukebox.IsPlaying, _jukebox.CurrentIndex, _jukebox.ListOfSongs())));
		}

		public void _addSongs(string songIds)
		{
			List<Song> songs = new List<Song>();
			foreach(string p in songIds.Split(','))
			{
				try
				{
					songs.Add(new Song(int.Parse(p)));
				}
				catch(Exception e)
				{
					Console.WriteLine("[JUKEBOXAPI] Error getting songs to add: " + e.Message);
				}
			}

			_jukebox.AddSongs(songs);
		}

		public void _removeSongs(string songIds)
		{
			List<int> indices = new List<int>();
			foreach (string p in songIds.Split(','))
			{
				try
				{
					indices.Add(int.Parse(p));
				}
				catch (Exception e)
				{
					Console.WriteLine("[JUKEBOXAPI] Error getting songs to remove: " + e.Message);
				}
			}

			_jukebox.RemoveSongsAtIndexes(indices);
		}

		public void _move(string from, string to)
		{
			try
			{
				int fromI = int.Parse(from);
				int toI = int.Parse(to);
				_jukebox.MoveSong(fromI, toI);
			}
			catch(Exception e)
			{
				Console.WriteLine("[JUKEBOXAPI] Error moving songs: " + e.Message);
			}
		}
	}

	class JukeboxStatusResponse
	{
		private bool isPlaying;
		private int currentIndex;
		private double progress;
		private Jukebox _jukebox = Jukebox.Instance;

		public JukeboxStatusResponse(bool isplaying, int currentindex, double prog)
		{
			isPlaying = isplaying;
			currentIndex = currentindex;
			progress = prog;
		}

		[JsonProperty("isPlaying")]
		public bool IsPlaying
		{
			get
			{
				return _jukebox.IsPlaying;
			}
		}

		[JsonProperty("currentIndex")]
		public int CurrentIndex
		{
			get
			{
				return _jukebox.CurrentIndex;
			}
		}

		[JsonProperty("progress")]
		public double Progress
		{
			get
			{
				return _jukebox.Progress();
			}
		}
	}

	class JukeboxPlaylistResponse
	{
		private bool isPlaying;
		private int currentIndex;
		private List<MediaItem> songs;
		private Jukebox _jukebox = Jukebox.Instance;

		public JukeboxPlaylistResponse(bool isplaying, int currentindex, List<MediaItem> songz)
		{
			isPlaying = isplaying;
			currentIndex = currentindex;
			songs = songz;
		}

		[JsonProperty("isPlaying")]
		public bool IsPlaying
		{
			get
			{
				return _jukebox.IsPlaying;
			}
		}

		[JsonProperty("currentIndex")]
		public int CurrentIndex
		{
			get
			{
				return _jukebox.CurrentIndex;
			}
		}

		[JsonProperty("songs")]
		public List<MediaItem> Songs
		{
			get
			{
				return _jukebox.ListOfSongs();
			}
		}
	}
}
