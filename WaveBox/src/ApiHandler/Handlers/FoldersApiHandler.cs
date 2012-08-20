using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using WaveBox.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class FoldersApiHandler : IApiHandler
	{
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
			List<MediaItem> listOfMediaItems = new List<MediaItem>();

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
				var folder = new Folder(id);
				listOfFolders = folder.ListOfSubFolders();
				listOfMediaItems = folder.ListOfMediaItems();
			}
			else
			{
				// If no id parameter, return media folders
				listOfFolders = Folder.MediaFolders();
			}

			try
			{
				string json = JsonConvert.SerializeObject(new FoldersResponse(null, listOfFolders, listOfMediaItems), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				Console.WriteLine("[FOLDERAPI(1)] ERROR: " + e.ToString());
			}
		}

		private class FoldersResponse
		{
	        [JsonProperty("error")]
			public string Error { get; set; }

	        [JsonProperty("folders")]
			public List<Folder> Folders { get; set; }

	        [JsonProperty("mediaItems")]
			public List<MediaItem> MediaItems { get; set; }

			public FoldersResponse(string error, List<Folder> folders, List<MediaItem> mediaItems)
			{
				Error = error;
				Folders = folders;
				MediaItems = mediaItems;
			}
		}
	}
}
