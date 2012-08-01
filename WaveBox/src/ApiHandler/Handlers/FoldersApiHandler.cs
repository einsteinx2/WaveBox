using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WaveBox.DataModel.Model;
using WaveBox.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class FoldersApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public FoldersApiHandler(UriWrapper uri, HttpProcessor processor, int userId)
		{
			Processor = processor;
			Uri = uri;
		}

		public void Process()
		{
			List<Folder> listOfFolders = new List<Folder>();
			List<Song> listOfSongs = new List<Song>();
			string json = "";

			var stuff = Uri.UriPart(2);
			Console.WriteLine("[FOLDERAPI]: UriPart(2) = " + stuff);

			// if the second part of the URI is null or contains GET parameters, we should ignore it and send the folder listing.
			if (stuff == null || stuff.Contains('='))
			{
				listOfFolders = Folder.TopLevelFolders();
			}
			else
			{
				var folder = new Folder(Convert.ToInt32(stuff));
				listOfFolders = folder.ListOfSubFolders();
				listOfSongs = folder.ListOfSongs();
			}

			try
			{
				json = JsonConvert.SerializeObject(new FoldersResponse(null, listOfFolders, listOfSongs), Formatting.None);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				Console.WriteLine("[FOLDERAPI(1)] ERROR: " + e.ToString());
			}
		}
	}

	class FoldersResponse
	{
        [JsonProperty("error")]
		public string Error { get; set; }

        [JsonProperty("folders")]
		public List<Folder> Folders { get; set; }

        [JsonProperty("songs")]
		public List<Song> Songs { get; set; }

		public FoldersResponse(string error, List<Folder> folders, List<Song> songs)
		{
			Error = error;
			Folders = folders;
			Songs = songs;
		}
	}
}
