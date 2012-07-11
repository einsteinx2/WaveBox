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
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public FoldersApiHandler(UriWrapper uriW, HttpProcessor sh, int userId)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			List<Folder> listOfFolders = new List<Folder>();
			List<Song> listOfSongs = new List<Song>();
			string json = "";

			var stuff = _uriW.getUriPart(2);

			// if the second part of the URI is null or contains GET parameters, we should ignore it and send the folder listing.
			if (stuff == null || stuff.Contains('='))
			{
				listOfFolders = Folder.topLevelFolders();
			}
			else
			{
				var folder = new Folder(Convert.ToInt32(stuff));
				listOfFolders = folder.listOfSubFolders();
				listOfSongs = folder.listOfSongs();
			}

			json = JsonConvert.SerializeObject(new FoldersResponse(null, listOfFolders, listOfSongs), Formatting.None);
			PmsHttpServer.sendJson(_sh, json);
		}
	}

	class FoldersResponse
	{
		private string _error;
		public string error
		{
			get
			{
				return _error;
			}
			set
			{
				_error = value;
			}
		}

		private List<Folder> _folders;
		public List<Folder> folders
		{
			get
			{
				return _folders;
			}
			set
			{
				_folders = value;
			}
		}

		private List<Song> _songs;
		public List<Song> songs
		{
			get
			{
				return _songs;
			}

			set
			{
				_songs = value;
			}
		}

		public FoldersResponse(string Error, List<Folder> Folders, List<Song> Songs)
		{
			error = Error;
			folders = Folders;
			songs = Songs;
		}
	}
}
