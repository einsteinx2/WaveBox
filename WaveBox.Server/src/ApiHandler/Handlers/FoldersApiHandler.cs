using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Model;
using WaveBox.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.ApiResponse;
using WaveBox.Core;
using WaveBox.Core.Static;

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
				containingFolder = Injection.Kernel.Get<IFolderRepository>().FolderForId(id);
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
					listOfFolders = Injection.Kernel.Get<IFolderRepository>().MediaFolders();
				}
				else
				{
					// They didn't ask for media folders, so send top level folders
					listOfFolders = Injection.Kernel.Get<IFolderRepository>().TopLevelFolders();
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
	}
}
