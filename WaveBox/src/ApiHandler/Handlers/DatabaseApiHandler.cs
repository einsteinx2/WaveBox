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
	class DatabaseApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		
		public DatabaseApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}
		
		public void Process()
		{	
			/*// Try to get the folder id
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
				//listOfMediaItems = folder.ListOfMediaItems();
				listOfSongs = folder.ListOfSongs();
				listOfVideos = folder.ListOfVideos();
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
				Console.WriteLine("[FOLDERAPI(1)] ERROR: " + e);
			}*/
		}
		
		private class DatabaseResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }
			
			[JsonProperty("lastUpdate")]
			public long LastUpdate { get; set; }

			public DatabaseResponse(string error, long lastUpdate)
			{
				Error = error;
				LastUpdate = lastUpdate;
			}
		}
	}
}
