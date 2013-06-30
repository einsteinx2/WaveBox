using System;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.Model;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class PlaylistsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for FoldersApiHandler
		/// </summary>
		public PlaylistsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process returns a JSON response list of folders
		/// </summary>
		public void Process()
		{
			// Generate return lists of folders, songs, videos
			string error = null;
			List<Playlist> listOfPlaylists = new List<Playlist>();
			List<IMediaItem> listOfMediaItems = new List<IMediaItem>();

			// Try to get the playlist id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			// Try to get the action
			string action = "list";
			if (Uri.Parameters.ContainsKey("action"))
			{
				action = Uri.Parameters["action"];
			}

			if (success)
			{
				// Return the playlist for this id
				Playlist playlist = new Playlist.Factory().CreatePlaylist(id);
				if (ReferenceEquals(playlist.PlaylistId, null))
				{
					error = "Playlist does not exist";
				}
				else
				{
					switch (action)
					{
						case "delete":
							playlist.DeletePlaylist();
							break;

						case "add":
							// Try to get the itemIds to add them to the playlist if necessary
							List<int> itemIds = ParseItemIds();
							if (itemIds.Count == 0)
							{
								error = "Missing item ids";
							}
							else
							{
								playlist.AddMediaItems(itemIds);
								listOfMediaItems = playlist.ListOfMediaItems();
							}
							break;

						case "remove":
							List<int> removeIndexes = ParseIndexes();
							if (removeIndexes.Count == 0)
							{
								error = "No indexes supplied";
							}
							else
							{
								playlist.RemoveMediaItemAtIndexes(removeIndexes);
								listOfMediaItems = playlist.ListOfMediaItems();
							}
							break;

						case "move":
							List<int> moveIndexes = ParseIndexes();
							if (moveIndexes.Count == 0 || moveIndexes.Count % 2 != 0)
							{
								error = "Incorrect number of indexes supplied";
							}
							else
							{
								for (int i = 0; i < moveIndexes.Count; i+=2)
								{
									int fromIndex = moveIndexes[i];
									int toIndex = moveIndexes[i+1];
									logger.Info("Calling move media item fromIndex: " + fromIndex + " toIndex: " + toIndex);
									playlist.MoveMediaItem(fromIndex, toIndex);
								}
								listOfMediaItems = playlist.ListOfMediaItems();
							}
							break;
						
						case "insert":
							List<int> insertItemIds = ParseItemIds();
							List<int> insertIndexes = ParseIndexes();
							if (insertItemIds.Count == 0 || insertItemIds.Count != insertIndexes.Count)
							{
								error = "Incorrect number of items and indexes supplied";
							}
							else
							{
								for (int i = 0; i < insertItemIds.Count; i++)
								{
									int itemId = insertItemIds[i];
									int index = insertIndexes[i];
									playlist.InsertMediaItem(itemId, index);
								}
								listOfMediaItems = playlist.ListOfMediaItems();
							}
							break;
						
						case "list":
						default:
							listOfMediaItems = playlist.ListOfMediaItems();
							break;
					}

					// Return the playlist so the client can use the info
					listOfPlaylists.Add(playlist);
				}
			}
			else if (action.Equals("create"))
			{
				// Try to get the name
				string name = null;
				if (Uri.Parameters.ContainsKey("name"))
				{
					name = HttpUtility.UrlDecode(Uri.Parameters["name"]);
				}

				if (ReferenceEquals(name, null))
				{
					// No name provided, so error
					error = "No name provided";
				}
				else
				{
					Playlist playlist = new Playlist.Factory().CreatePlaylist(name);

					if (ReferenceEquals(playlist.ItemId, null))
					{
						// Looks like this name is unused, so create the playlist
						playlist.CreatePlaylist();

						// Try to get the itemIds to add them to the playlist if necessary
						List<int> itemIds = ParseItemIds();
						if (itemIds.Count > 0)
						{
							playlist.AddMediaItems(itemIds);
						}

						listOfMediaItems = playlist.ListOfMediaItems();
						listOfPlaylists.Add(playlist);
					}
					else
					{
						error = "Name already in use";
					}
				}
			}
			else
			{
				// No id parameter, so return all playlists
				listOfPlaylists = Playlist.AllPlaylists();
			}

			// Return all results
			try
			{
				string json = JsonConvert.SerializeObject(new PlaylistsResponse(error, listOfPlaylists, listOfMediaItems), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		private List<int> ParseItemIds()
		{
			// Try to get the itemIds
			List<int> itemIds = new List<int>();
			if (Uri.Parameters.ContainsKey("itemIds"))
			{
				string[] itemIdStrings = Uri.Parameters["itemIds"].Split(',');

				foreach (string itemIdString in itemIdStrings)
				{
					int itemId;
					if (Int32.TryParse(itemIdString, out itemId))
					{
						itemIds.Add(itemId);
					}
				}
			}

			return itemIds;
		}

		private List<int> ParseIndexes()
		{
			// Try to get the itemIds
			List<int> itemIds = new List<int>();
			if (Uri.Parameters.ContainsKey("indexes"))
			{
				string[] itemIdStrings = Uri.Parameters["indexes"].Split(',');

				foreach (string itemIdString in itemIdStrings)
				{
					int itemId;
					if (Int32.TryParse(itemIdString, out itemId))
					{
						itemIds.Add(itemId);
					}
				}
			}

			return itemIds;
		}

		private class PlaylistsResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("playlists")]
			public List<Playlist> Playlists { get; set; }

			[JsonProperty("mediaItems")]
			public List<IMediaItem> MediaItems { get; set; }

			public PlaylistsResponse(string error, List<Playlist> playlists, List<IMediaItem> mediaItems)
			{
				Error = error;
				Playlists = playlists;
				MediaItems = mediaItems;
			}
		}
	}
}

