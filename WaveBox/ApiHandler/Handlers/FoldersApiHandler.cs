using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WaveBox.DataModel.Model;
using Bend.Util;

namespace WaveBox.ApiHandler.Handlers
{
	class FoldersApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		public FoldersApiHandler(UriWrapper uri, HttpProcessor processor, long userId)
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

			json = JsonConvert.SerializeObject(new FoldersResponse(null, listOfFolders, listOfSongs), Formatting.None);
			WaveBoxHttpServer.sendJson(Processor, json);
		}
	}

	class FoldersResponse
	{
		public string Error { get; set; }
		public List<Folder> Folders { get; set; }
		public List<Song> Songs { get; set; }

		public FoldersResponse(string error, List<Folder> folders, List<Song> songs)
		{
			Error = error;
			Folders = folders;
			Songs = songs;
		}
	}
}
