using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Service.Services.Http;
using WaveBox.Static;
using WaveBox.Core.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class PlaylistsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "playlists"; } }

		// Standard permissions
		public bool CheckPermission(User user, string action)
		{
			switch (action)
			{
				// Write
				case "add":
				case "create":
				case "delete":
				case "insert":
				case "move":
				case "remove":
					return user.HasPermission(Role.User);
				// Read
				case "read":
				default:
					return user.HasPermission(Role.Test);
			}
		}

		/// <summary>
		/// Process returns a JSON response list of playlists
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Generate return lists of playlists, media items in them
			IList<Playlist> listOfPlaylists = new List<Playlist>();
			IList<IMediaItem> listOfMediaItems = new List<IMediaItem>();
			PairList<string, int> sectionPositions = new PairList<string, int>();

			// The playlist to perform actions
			Playlist playlist;

			// Check for playlist creation
			if (uri.Action == "create")
			{
				// Try to get the name
				string name = null;
				if (uri.Parameters.ContainsKey("name"))
				{
					name = HttpUtility.UrlDecode(uri.Parameters["name"]);
				}

				// Verify non-null name
				if (name == null)
				{
					processor.WriteJson(new PlaylistsResponse("Parameter 'name' required for playlist creation", null, null, null));
					return;
				}

				// Verify name not already in use
				playlist = Injection.Kernel.Get<IPlaylistRepository>().PlaylistForName(name);
				if (playlist.ItemId != null)
				{
					processor.WriteJson(new PlaylistsResponse("Playlist name '" + name + "' already in use", null, null, null));
					return;
				}

				// Looks like this name is unused, so create the playlist
				playlist.PlaylistName = name;
				playlist.CreatePlaylist();

				// Try to get the itemIds to add them to the playlist if necessary
				IList<int> itemIds = this.ParseItemIds(uri);
				if (itemIds.Count > 0)
				{
					playlist.AddMediaItems(itemIds);
				}

				listOfMediaItems = playlist.ListOfMediaItems();
				listOfPlaylists.Add(playlist);

				// Return newly created playlist
				processor.WriteJson(new PlaylistsResponse(null, listOfPlaylists, listOfMediaItems, sectionPositions));
				return;
			}

			// If not creating playlist, and no ID, or reading, return all playlists
			if (uri.Id == null || uri.Action == "read")
			{
				listOfPlaylists = Injection.Kernel.Get<IPlaylistRepository>().AllPlaylists();
				sectionPositions = Utility.SectionPositionsFromSortedList(new List<IGroupingItem>(listOfPlaylists.Select(c => (IGroupingItem)c)));
				processor.WriteJson(new PlaylistsResponse(null, listOfPlaylists, listOfMediaItems, sectionPositions));
				return;
			}

			// If ID, return the playlist for this ID
			playlist = Injection.Kernel.Get<IPlaylistRepository>().PlaylistForId((int)uri.Id);
			if (playlist.PlaylistId == null)
			{
				processor.WriteJson(new PlaylistsResponse("Playlist does not exist", null, null, null));
				return;
			}

			// add - add items to a playlist
			if (uri.Action == "add")
			{
				// Try to get the itemIds to add them to the playlist if necessary
				IList<int> itemIds = this.ParseItemIds(uri);
				if (itemIds.Count == 0)
				{
					processor.WriteJson(new PlaylistsResponse("No item IDs found in URL", null, null, null));
					return;
				}

				// Iterate item IDs
				for (int i = 0; i < itemIds.Count; i++)
				{
					// Store ID
					int itemId = itemIds[i];

					// List of songs
					IList<IMediaItem> songs = null;

					// Iterate each item type in the list, adding items
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
							processor.WriteJson(new PlaylistsResponse("Invalid item type at index: " + i, null, null, null));
							return;
					}
				}

				// Grab everything just put in the playlist
				listOfPlaylists.Add(playlist);
				listOfMediaItems = playlist.ListOfMediaItems();

				processor.WriteJson(new PlaylistsResponse(null, listOfPlaylists, listOfMediaItems, sectionPositions));
				return;
			}

			// delete - delete a playlist
			if (uri.Action == "delete")
			{
				playlist.DeletePlaylist();
				listOfPlaylists.Add(playlist);

				processor.WriteJson(new PlaylistsResponse(null, listOfPlaylists, listOfMediaItems, sectionPositions));
				return;
			}

			// insert - insert item in playlist at specified index
			if (uri.Action == "insert")
			{
				IList<int> insertItemIds = this.ParseItemIds(uri);
				IList<int> insertIndexes = this.ParseIndexes(uri);
				if (insertItemIds.Count == 0 || insertItemIds.Count != insertIndexes.Count)
				{
					processor.WriteJson(new PlaylistsResponse("Incorrect number of items and indices supplied for action 'insert'", null, null, null));
					return;
				}

				// Add media items and specified indices
				for (int i = 0; i < insertItemIds.Count; i++)
				{
					int itemId = insertItemIds[i];
					int index = insertIndexes[i];
					playlist.InsertMediaItem(itemId, index);
				}

				// Return playlist with media items
				listOfPlaylists.Add(playlist);
				listOfMediaItems = playlist.ListOfMediaItems();

				processor.WriteJson(new PlaylistsResponse(null, listOfPlaylists, listOfMediaItems, sectionPositions));
				return;
			}

			// list/read - list all of the items in a playlist ("list" keep for compatibility)
			if (uri.Action == "list" || uri.Action == "read")
			{
				listOfPlaylists.Add(playlist);
				listOfMediaItems = playlist.ListOfMediaItems();

				processor.WriteJson(new PlaylistsResponse(null, listOfPlaylists, listOfMediaItems, sectionPositions));
				return;
			}

			// move - move an item in the playlist
			if (uri.Action == "move")
			{
				IList<int> moveIndexes = this.ParseIndexes(uri);
				if (moveIndexes.Count == 0 || moveIndexes.Count % 2 != 0)
				{
					processor.WriteJson(new PlaylistsResponse("Incorrect number of indices supplied for action 'move'", null, null, null));
					return;
				}

				// Move media items in playlist
				for (int i = 0; i < moveIndexes.Count; i += 2)
				{
					int fromIndex = moveIndexes[i];
					int toIndex = moveIndexes[i+1];
					playlist.MoveMediaItem(fromIndex, toIndex);
				}

				listOfPlaylists.Add(playlist);
				listOfMediaItems = playlist.ListOfMediaItems();

				processor.WriteJson(new PlaylistsResponse(null, listOfPlaylists, listOfMediaItems, sectionPositions));
				return;
			}

			// remove - remove items from playlist
			if (uri.Action == "remove")
			{
				IList<int> removeIndexes = this.ParseIndexes(uri);
				if (removeIndexes.Count == 0)
				{
					processor.WriteJson(new PlaylistsResponse("No indices supplied for action 'remove'", null, null, null));
					return;
				}

				playlist.RemoveMediaItemAtIndexes(removeIndexes);

				listOfPlaylists.Add(playlist);
				listOfMediaItems = playlist.ListOfMediaItems();

				processor.WriteJson(new PlaylistsResponse(null, listOfPlaylists, listOfMediaItems, sectionPositions));
				return;
			}

			// Finally, invalid action supplied
			processor.WriteJson(new PlaylistsResponse("Invalid action: " + uri.Action, listOfPlaylists, listOfMediaItems, sectionPositions));
			return;
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
