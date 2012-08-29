using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using WaveBox.Http;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	class JukeboxApiHandler : IApiHandler
	{
		private Jukebox Juke;
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		//private int _userId;

		public JukeboxApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Juke = Jukebox.Instance;
			Processor = processor;
			Uri = uri;
		}

		public void Process()
        {
            int index = 0;
            string s = "";

            if (Uri.Parameters.ContainsKey("action"))
            {
                string action = null;
                Uri.Parameters.TryGetValue("action", out action);

                if (new string[] {"play", "pause", "stop", "prev", "next", "status", "add", "remove", "move", "clear"}.Contains(action))
                {
                    switch(action)
                    {
                        case "play":
                            string indexString = null;
                            if(Uri.Parameters.ContainsKey("index"))
                            {
                                Uri.Parameters.TryGetValue("index", out indexString);
                                Int32.TryParse(indexString, out index);
                            }

                            if 
                                (indexString == null) Juke.Play();
                            else 
                                Juke.PlaySongAtIndex(index);

                            Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true)));
                            break;
                        case "pause":
                            Juke.Pause();
                            Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true)));
                            break;
                        case "stop":
                            Juke.Stop();
                            Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true)));
                            break;
                        case "prev":
                            Juke.Prev();
                            Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true)));
                            break;
                        case "next":
                            Juke.Next();
                            Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true)));
                            break;
                        case "status":
                            _status();
                            break;
                        case "playlist":
                            _playlist();
                            break;
                        case "add":
                            if (Uri.Parameters.ContainsKey("id"))
                            {
                                s = "";
                                Uri.Parameters.TryGetValue("id", out s);
                                if(AddSongs(s))
                                    Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, true, false)));
                            }
                            break;
                        case "remove":
                            if (Uri.Parameters.ContainsKey("index"))
                            {
                                s = "";
                                Uri.Parameters.TryGetValue("index", out s);
                                RemoveSongs(s);
                                Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, true, false)));
                            }
                            break;
                        case "move":
                            if (Uri.Parameters.ContainsKey("index"))
                            {
                                string[] arr = null;
                                if(Uri.Parameters.TryGetValue("index", out s))
                                {
                                    arr = s.Split(',');
                                    if(arr.Length == 2)
                                    {
                                        if(Move(arr[0], arr[1]))
                                        {
                                            Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, true, false)));
                                        }
                                    }
                                    else 
                                    {
                                        Console.WriteLine("[JUKEBOXAPI] Move: Invalid number of indices");
                                        Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("Invalid number of indices for action 'move'", false, false)));
                                    }

                                }
                            }
                            else 
                            {
                                Console.WriteLine("[JUKEBOXAPI] Move: Missing 'index' parameter");
                                Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("Missing 'index' parameter for action 'move'", false, false)));
                            }
                            break;
                        case "clear":
                            Juke.ClearPlaylist();
                            Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, false)));
                            break;
                        default: break;

                    }
                }
            }
		}

		public void _status()
		{
			Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true)));
		}

		public void _playlist()
		{
			Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, true, false)));
		}

		public bool AddSongs(string songIds)
		{
            bool allSongsAddedSuccessfully = true;
			List<Song> songs = new List<Song>();
			foreach(string p in songIds.Split(','))
			{
				try
				{
                    if(Item.ItemTypeForItemId(int.Parse(p)) == ItemType.Song)
                    {
                        Song s = new Song(int.Parse(p));
    					songs.Add(s);
                    }
                    else
                    {
                        allSongsAddedSuccessfully = false;
                    }
				}
				catch(Exception e)
				{
					Console.WriteLine("[JUKEBOXAPI(2)] Error getting songs to add: " + e.Message);
                    Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("Error getting songs to add", false, false)));
                    return false;
				}
			}
			Juke.AddSongs(songs);

            if(!allSongsAddedSuccessfully)
                Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("One or more items provided were not of the appropriate type and were not added to the playlist.", true, false)));
            return allSongsAddedSuccessfully;
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

		public bool Move(string from, string to)
		{
			try
			{
				int fromI = int.Parse(from);
				int toI = int.Parse(to);
				Juke.MoveSong(fromI, toI);
                return true;
			}
			catch(Exception e)
			{
				Console.WriteLine("[JUKEBOXAPI(4)] Error moving songs: " + e.Message);
                return false;
			}
		}

		private class JukeboxStatus
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

		private class JukeboxResponse
		{
            bool includePlaylist, includeStatus;

            [JsonProperty("error")]
            public string Error { get; set; }

			[JsonProperty("jukeboxStatus")]
            public JukeboxStatus JukeboxStatus 
            { 
                get 
                { 
                    if(includeStatus) 
                        return new JukeboxStatus(); 
                    else 
                        return null;
                } 
            }

			[JsonProperty("jukeboxPlaylist")]
			public List<MediaItem> JukeboxPlaylist 
            { 
                get 
                { 
                    if(includePlaylist)
                        return Jukebox.Instance.ListOfSongs(); 
                    else
                        return null;
                } 
            }

            public JukeboxResponse(string error, bool _includePlaylist, bool _includeStatus)
            {

                Error = error;
                includePlaylist = _includePlaylist;
                includeStatus = _includeStatus;
            }
		}
	}
}
