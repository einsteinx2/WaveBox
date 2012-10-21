using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using WaveBox.Http;
using NLog;

namespace WaveBox.ApiHandler.Handlers
{
	class FoldersApiHandler : IApiHandler
	{		
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public FoldersApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			List<Folder> listOfFolders = new List<Folder>();
			//List<IMediaItem> listOfMediaItems = new List<IMediaItem>();
			List<Song> listOfSongs = new List<Song>();
			List<Video> listOfVideos = new List<Video>();

			// Try to get the folder id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			if (success)
			{
				// Return the folder for this id
				Folder folder = new Folder(id);
				listOfFolders = folder.ListOfSubFolders();

                if(Uri.Parameters.ContainsKey("recursiveMedia") && this.IsTrue(Uri.Parameters["recursiveMedia"]))
                {
                    listOfSongs = new List<Song>();
                    listOfVideos = new List<Video>();

                    // recursively add media in all subfolders to the list.
                    Action<Folder> recurse = null;
                    recurse = f =>
                    {
                        listOfSongs.AddRange(f.ListOfSongs());
                        listOfVideos.AddRange(f.ListOfVideos());
                        foreach(var subf in f.ListOfSubFolders())
                        {
                            recurse(subf);
                        }
                    };
                    recurse(folder);
                }
                else 
                {
    				//listOfMediaItems = folder.ListOfMediaItems();
    				listOfSongs = folder.ListOfSongs();
    				listOfVideos = folder.ListOfVideos();
                }
			}
			else
			{
				// No id parameter
				if (Uri.Parameters.ContainsKey("mediaFolders") && this.IsTrue(Uri.Parameters["mediaFolders"]))
				{
					// They asked for the media folders
					listOfFolders = Folder.MediaFolders();
				}
				else
				{
					// They didn't ask for media folders, so send top level folders
					listOfFolders = Folder.TopLevelFolders();
				}
			}

			try
			{
				string json = JsonConvert.SerializeObject(new FoldersResponse(null, listOfFolders, listOfSongs, listOfVideos), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				logger.Error("[FOLDERAPI(1)] ERROR: " + e);
			}
		}

		private class FoldersResponse
		{
	        [JsonProperty("error")]
			public string Error { get; set; }

	        [JsonProperty("folders")]
			public List<Folder> Folders { get; set; }

	        //[JsonProperty("mediaItems")]
			//public List<IMediaItem> MediaItems { get; set; }

			[JsonProperty("songs")]
			public List<Song> Songs { get; set; }

			[JsonProperty("videos")]
			public List<Video> Videos { get; set; }

			public FoldersResponse(string error, List<Folder> folders, List<Song> songs, List<Video>videos)
			{
				Error = error;
				Folders = folders;
				Songs = songs;
				Videos = videos;
			}
		}
	}
}
