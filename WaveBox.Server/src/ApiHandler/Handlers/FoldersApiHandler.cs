using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class FoldersApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "folders"; } set { } }

		/// <summary>
		/// Process returns a JSON response list of folders
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Generate return lists of folders, songs, videos
			IList<Folder> listOfFolders = new List<Folder>();
			IList<Song> listOfSongs = new List<Song>();
			IList<Video> listOfVideos = new List<Video>();
			Folder containingFolder = null;
			bool recursive = false;

			// Try to get the folder id
			bool success = false;
			int id = 0;
			if (uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(uri.Parameters["id"], out id);
			}

			if (success)
			{
				// Return the folder for this id
				containingFolder = Injection.Kernel.Get<IFolderRepository>().FolderForId(id);
				listOfFolders = containingFolder.ListOfSubFolders();

				if (uri.Parameters.ContainsKey("recursiveMedia") && uri.Parameters["recursiveMedia"].IsTrue())
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
				if (uri.Parameters.ContainsKey("mediaFolders") && uri.Parameters["mediaFolders"].IsTrue())
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
				processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
