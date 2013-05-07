using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using WaveBox.TcpServer.Http;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	class JukeboxApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private Jukebox Juke;
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for JukeboxApiHandler class
		/// </summary>
		public JukeboxApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Juke = Jukebox.Instance;
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process returns whether a specific call the Jukebox API was successful or not
		/// </summary>
		public void Process()
		{
			int index = 0;
			string s = "";

			// Jukebox calls must contain an action
			if (Uri.Parameters.ContainsKey("action"))
			{
				string action = null;
				Uri.Parameters.TryGetValue("action", out action);

				// Look for valid actions within the parameters
				if (new string[] {"play", "pause", "stop", "prev", "next", "status", "playlist", "add", "remove", "move", "clear"}.Contains(action))
				{
					switch (action)
					{
						case "play":
							string indexString = null;
							if (Uri.Parameters.ContainsKey("index"))
							{
								Uri.Parameters.TryGetValue("index", out indexString);
								Int32.TryParse(indexString, out index);
							}
							
							if (indexString == null)
							{
								Juke.Play();
							}
							else
							{
								Juke.PlaySongAtIndex(index);
							}

							Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true), Settings.JsonFormatting));
							break;
						case "pause":
							Juke.Pause();
							Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true), Settings.JsonFormatting));
							break;
						case "stop":
							Juke.Stop();
							Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true), Settings.JsonFormatting));
							break;
						case "prev":
							Juke.Prev();
							Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true), Settings.JsonFormatting));
							break;
						case "next":
							Juke.Next();
							Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true), Settings.JsonFormatting));
							break;
						case "status":
							Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, true), Settings.JsonFormatting));
							break;
						case "playlist":
							Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, true, false), Settings.JsonFormatting));
							break;
						case "add":
							if (Uri.Parameters.ContainsKey("id"))
							{
								s = "";
								Uri.Parameters.TryGetValue("id", out s);
								if (AddSongs(s))
								{
									Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, true, false), Settings.JsonFormatting));
								}
							}
							break;
						case "remove":
							if (Uri.Parameters.ContainsKey("index"))
							{
								s = "";
								Uri.Parameters.TryGetValue("index", out s);
								RemoveSongs(s);
								Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, true, false), Settings.JsonFormatting));
							}
							break;
						case "move":
							if (Uri.Parameters.ContainsKey("index"))
							{
								string[] arr = null;
								if (Uri.Parameters.TryGetValue("index", out s))
								{
									arr = s.Split(',');
									if (arr.Length == 2)
									{
										if (Move(arr[0], arr[1]))
										{
											Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, true, false), Settings.JsonFormatting));
										}
									}
									else 
									{
										if (logger.IsInfoEnabled) logger.Info("Move: Invalid number of indices");
										Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("Invalid number of indices for action 'move'", false, false), Settings.JsonFormatting));
									}

								}
							}
							else 
							{
								if (logger.IsInfoEnabled) logger.Info("Move: Missing 'index' parameter");
								Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("Missing 'index' parameter for action 'move'", false, false), Settings.JsonFormatting));
							}
							break;
						case "clear":
							Juke.ClearPlaylist();
							Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse(null, false, false), Settings.JsonFormatting));
							break;
						default:
							// This should never happen, unless we forget to add a case.
							Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("You broke WaveBox", false, false), Settings.JsonFormatting));
							break;
					}
				}
				else
				{
					// Else, invalid action specified, return an error
					Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("Invalid action '" + action + "' specified", false, false), Settings.JsonFormatting));
				}
			}
			else
			{
				// Else, no action provided, return an error
				Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("No action specified for jukebox", false, false), Settings.JsonFormatting));
			}
		}

		/// <summary>
		/// Add comma-separated list of songs to the Jukebox
		/// </summary>
		public bool AddSongs(string songIds)
		{
			bool allSongsAddedSuccessfully = true;
			List<Song> songs = new List<Song>();
			foreach (string p in songIds.Split(','))
			{
				try
				{
					if (Item.ItemTypeForItemId(int.Parse(p)) == ItemType.Song)
					{
						Song s = new Song(int.Parse(p));
						songs.Add(s);
					}
					else
					{
						allSongsAddedSuccessfully = false;
					}
				}
				catch (Exception e)
				{
					logger.Error("Error getting songs to add: ", e);
					Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("Error getting songs to add", false, false), Settings.JsonFormatting));
					return false;
				}
			}
			Juke.AddSongs(songs);

			if (!allSongsAddedSuccessfully)
			{
				Processor.WriteJson(JsonConvert.SerializeObject(new JukeboxResponse("One or more items provided were not of the appropriate type and were not added to the playlist.", true, false), Settings.JsonFormatting));
			}

			return allSongsAddedSuccessfully;
		}

		/// <summary>
		/// Remove comma-separated list of songs from Jukebox
		/// </summary>
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
					logger.Error("Error getting songs to remove: ", e);
				}
			}

			Juke.RemoveSongsAtIndexes(indices);
		}

		/// <summary>
		/// Move song in Jukebox
		/// </summary>
		public bool Move(string from, string to)
		{
			try
			{
				int fromI = int.Parse(from);
				int toI = int.Parse(to);
				Juke.MoveSong(fromI, toI);
				return true;
			}
			catch (Exception e)
			{
				logger.Error("Error moving songs: ", e);
				return false;
			}
		}

		private class JukeboxStatus
		{
			[JsonProperty("state")]
			public string State
			{
				get
				{
					return Jukebox.Instance.State.ToString();
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
					if (includeStatus)
					{
						return new JukeboxStatus();
					}
					else
					{
						return null;
					}
				} 
			}

			[JsonProperty("jukeboxPlaylist")]
			public List<IMediaItem> JukeboxPlaylist 
			{ 
				get 
				{ 
					if (includePlaylist)
					{
						return Jukebox.Instance.ListOfSongs();
					}
					else
					{
						return null;
					}
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
