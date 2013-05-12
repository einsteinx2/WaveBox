using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WaveBox.Singletons;
using WaveBox.Model;
using WaveBox.TcpServer.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class FoldersApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for FoldersApiHandler
		/// </summary>
		public FoldersApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
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
			List<Folder> listOfFolders = new List<Folder>();
			List<Song> listOfSongs = new List<Song>();
			List<Video> listOfVideos = new List<Video>();
			Folder containingFolder = null;

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
				containingFolder = new Folder(id);
				listOfFolders = containingFolder.ListOfSubFolders();

				// Recursively add media items from sub-folders
				if (Uri.Parameters.ContainsKey("recursiveMedia") && Utility.IsTrue(Uri.Parameters["recursiveMedia"]))
				{
					listOfSongs = new List<Song>();
					listOfVideos = new List<Video>();

					// Recursively add media in all subfolders to the list.
					Action<Folder> recurse = null;
					recurse = f =>
					{
						listOfSongs.AddRange(f.ListOfSongs());
						listOfVideos.AddRange(f.ListOfVideos());
						foreach (var subf in f.ListOfSubFolders())
						{
							recurse(subf);
						}
					};
					recurse(containingFolder);
				}
				else 
				{
					// If not recursing, grab media in current folder
					listOfSongs = containingFolder.ListOfSongs();
					listOfVideos = containingFolder.ListOfVideos();
				}
			}
			else
			{
				// No id parameter
				if (Uri.Parameters.ContainsKey("mediaFolders") && Utility.IsTrue(Uri.Parameters["mediaFolders"]))
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

			// Return all results
			try
			{
				string json = JsonConvert.SerializeObject(new FoldersResponse(null, containingFolder, listOfFolders, listOfSongs, listOfVideos), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		private class FoldersResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("folders")]
			public List<Folder> Folders { get; set; }

			[JsonProperty("containingFolder")]
			public Folder ContainingFolder { get; set; }

			[JsonProperty("songs")]
			public List<Song> Songs { get; set; }

			[JsonProperty("videos")]
			public List<Video> Videos { get; set; }

			public FoldersResponse(string error, Folder containingFolder, List<Folder> folders, List<Song> songs, List<Video>videos)
			{
				Error = error;
				ContainingFolder = containingFolder;
				Folders = folders;
				Songs = songs;
				Videos = videos;
			}
		}
	}
}
