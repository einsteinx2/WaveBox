using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using WaveBox.HttpServer;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	class JukeboxApiHandler : IApiHandler
	{
		private Jukebox Juke;
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		//private int _userId;

		public JukeboxApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Juke = Jukebox.Instance;
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			int index = 0;
			string s = "";

			string part2 = Uri.UriPart(2);
			//string part3 = Uri.UriPart(3);

			switch(part2)
			{
				case "play":
					string p3 = null;
					if(Uri.UriPart(3) != null)
					{
						try
						{
							p3 = Uri.UriPart(3);
							if(p3 != null)
								index = int.Parse(Uri.UriPart(3));
						}
						catch (Exception e)
						{
							Console.WriteLine("[JUKEBOXAPI(1)] (play) Error parsing part three of URI: " + e.Message);
						}
					}

					if 
						(p3 == null) Juke.Play();
					else 
						Juke.PlaySongAtIndex(index);
					break;
				case "pause":
					Juke.Pause();
					break;
				case "stop":
					Juke.Stop();
					break;
				case "prev":
					Juke.Prev();
					break;
				case "next":
					Juke.Next();
					break;
				case "status":
					_status();
					break;
				case "playlist":
					_playlist();
					break;
				case "add":
					if (Uri.Parameters.ContainsKey("i"))
					{
						s = "";
						Uri.Parameters.TryGetValue("i", out s);
						AddSongs(s);
					}
					break;
				case "remove":
					if (Uri.Parameters.ContainsKey("i"))
					{
						s = "";
						Uri.Parameters.TryGetValue("i", out s);
						RemoveSongs(s);
					}
					break;
				case "move":
					if (Uri.Parameters.ContainsKey("from") && Uri.Parameters.ContainsKey("to"))
					{
						string to, from;
						Uri.Parameters.TryGetValue("from", out from);
						Uri.Parameters.TryGetValue("to", out to);

						if (to != null && from != null)
						{
							Move(to, from);
						}
					}
					break;
				case "clear":
					Juke.ClearPlaylist();
					break;
				default: break;

			}
		}

		public void _status()
		{
			WaveBoxHttpServer.sendJson(Processor, JsonConvert.SerializeObject(new JukeboxStatusResponse()));
		}

		public void _playlist()
		{
			WaveBoxHttpServer.sendJson(Processor, JsonConvert.SerializeObject(new JukeboxPlaylistResponse()));
		}

		public void AddSongs(string songIds)
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
					Console.WriteLine("[JUKEBOXAPI(2)] Error getting songs to add: " + e.Message);
				}
			}

			Juke.AddSongs(songs);
		}

		public void RemoveSongs(string songIds)
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
					Console.WriteLine("[JUKEBOXAPI(3)] Error getting songs to remove: " + e.Message);
				}
			}

			Juke.RemoveSongsAtIndexes(indices);
		}

		public void Move(string from, string to)
		{
			try
			{
				int fromI = int.Parse(from);
				int toI = int.Parse(to);
				Juke.MoveSong(fromI, toI);
			}
			catch(Exception e)
			{
				Console.WriteLine("[JUKEBOXAPI(4)] Error moving songs: " + e.Message);
			}
		}
	}

	class JukeboxStatusResponse
	{
		[JsonProperty("isPlaying")]
		public bool IsPlaying
		{
			get
			{
				return Jukebox.Instance.IsPlaying;
			}
		}

		[JsonProperty("currentIndex")]
		public int CurrentIndex
		{
			get
			{
				return Jukebox.Instance.CurrentIndex;
			}
		}

		[JsonProperty("progress")]
		public double Progress
		{
			get
			{
				return Jukebox.Instance.Progress();
			}
		}
	}

	class JukeboxPlaylistResponse
	{
		[JsonProperty("isPlaying")]
		public bool IsPlaying { get { return Jukebox.Instance.IsPlaying; } }

		[JsonProperty("currentIndex")]
		public int CurrentIndex { get { return Jukebox.Instance.CurrentIndex; } }

		[JsonProperty("songs")]
		public List<MediaItem> Songs { get { return Jukebox.Instance.ListOfSongs(); } }
	}
}
