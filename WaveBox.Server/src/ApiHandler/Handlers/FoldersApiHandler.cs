using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.Model;
using WaveBox.Static;
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
			bool recursive = false;

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
				containingFolder = new Folder.Factory().CreateFolder(id);
				listOfFolders = containingFolder.ListOfSubFolders();

				if (Uri.Parameters.ContainsKey("recursiveMedia") && Uri.Parameters["recursiveMedia"].IsTrue())
				{
					recursive = true;
				}

				// Get it, son.
				listOfSongs = containingFolder.ListOfSongs(recursive);
				listOfVideos = containingFolder.ListOfVideos(recursive);
			}
			else
			{
				// No id parameter
				if (Uri.Parameters.ContainsKey("mediaFolders") && Uri.Parameters["mediaFolders"].IsTrue())
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
				string json = JsonConvert.SerializeObject(new FoldersResponse(null, containingFolder, listOfFolders, listOfSongs, listOfVideos), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
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
