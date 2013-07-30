using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;
using WaveBox.Core.Model.Repository;
using WaveBox.Core;

namespace WaveBox.ApiHandler.Handlers
{
	class PlaylistsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "playlists"; } set { } }

		/// <summary>
		/// Process returns a JSON response list of playlists
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Generate return lists of playlists, media items in them
			string error = null;
			IList<Playlist> listOfPlaylists = new List<Playlist>();
			IList<IMediaItem> listOfMediaItems = new List<IMediaItem>();

			// Try to get the playlist id
			bool success = false;
			int id = 0;
			if (uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(uri.Parameters["id"], out id);
			}

			// Try to get the action
			string action = "list";
			if (uri.Parameters.ContainsKey("action"))
			{
				action = uri.Parameters["action"];
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
						case "add":
							// Try to get the itemIds to add them to the playlist if necessary
							IList<int> itemIds = this.ParseItemIds(uri);
							if (itemIds.Count == 0)
							{
								error = "Missing item ids";
							}
							else
							{
								for (int i = 0; i < itemIds.Count; i++)
								{
									int itemId = itemIds[i];
									IList<IMediaItem> songs = null;
									switch (Injection.Kernel.Get<IItemRepository>().ItemTypeForItemId(itemId))
									{
										case ItemType.Folder:
											// get all the media items underneath this folder and add them
											// Use Select instead of ConvertAll: http://stackoverflow.com/questions/1571819/difference-between-select-and-convertall-in-c-sharp
											songs = Injection.Kernel.Get<IFolderRepository>().FolderForId(itemId).ListOfSongs(true).Select(x => (IMediaItem)x).ToList();
											playlist.AddMediaItems(songs);
											break;
										case ItemType.Artist:
											songs = Injection.Kernel.Get<IArtistRepository>().ArtistForId(itemId).ListOfSongs().Select(x => (IMediaItem)x).ToList();
											playlist.AddMediaItems(songs);
											break;
										case ItemType.Album:
											songs = Injection.Kernel.Get<IAlbumRepository>().AlbumForId(itemId).ListOfSongs().Select(x => (IMediaItem)x).ToList();
											playlist.AddMediaItems(songs);
											break;
										case ItemType.Song:
											playlist.AddMediaItem(Injection.Kernel.Get<ISongRepository>().SongForId(itemId));
											break;
										case ItemType.Video:
											playlist.AddMediaItem(Injection.Kernel.Get<IVideoRepository>().VideoForId(itemId));
											break;
										default:
											error = "Invalid item type at index: " + i;
											break;
									}
								}

								// Grab everything just put in the playlist
								listOfMediaItems = playlist.ListOfMediaItems();
							}
							break;
						case "delete":
							playlist.DeletePlaylist();
							break;
						case "insert":
							IList<int> insertItemIds = this.ParseItemIds(uri);
							IList<int> insertIndexes = this.ParseIndexes(uri);
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
							listOfMediaItems = playlist.ListOfMediaItems();
							break;
						case "move":
							IList<int> moveIndexes = this.ParseIndexes(uri);
							if (moveIndexes.Count == 0 || moveIndexes.Count % 2 != 0)
							{
								error = "Incorrect number of indexes supplied";
							}
							else
							{
								for (int i = 0; i < moveIndexes.Count; i += 2)
								{
									int fromIndex = moveIndexes[i];
									int toIndex = moveIndexes[i+1];
									logger.Info("Calling move media item fromIndex: " + fromIndex + " toIndex: " + toIndex);
									playlist.MoveMediaItem(fromIndex, toIndex);
								}
								listOfMediaItems = playlist.ListOfMediaItems();
							}
							break;
						case "remove":
							IList<int> removeIndexes = this.ParseIndexes(uri);
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
						default:
							error = "Invalid action: " + action;
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
				if (uri.Parameters.ContainsKey("name"))
				{
					name = HttpUtility.UrlDecode(uri.Parameters["name"]);
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
						IList<int> itemIds = this.ParseItemIds(uri);
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
				processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		private IList<int> ParseItemIds(UriWrapper uri)
		{
			// Try to get the itemIds
			IList<int> itemIds = new List<int>();
			if (uri.Parameters.ContainsKey("itemIds"))
			{
				string[] itemIdStrings = uri.Parameters["itemIds"].Split(',');

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

		private IList<int> ParseIndexes(UriWrapper uri)
		{
			// Try to get the itemIds
			IList<int> itemIds = new List<int>();
			if (uri.Parameters.ContainsKey("indexes"))
			{
				string[] itemIdStrings = uri.Parameters["indexes"].Split(',');

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
	}
}
