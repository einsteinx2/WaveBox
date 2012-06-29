using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using MediaFerry.DataModel.Model;
using Bend.Util;

namespace MediaFerry.ApiHandler.Handlers
{
	class FoldersApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public FoldersApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Artists: Only the ALL ALBUMS call has been implemented!");

			string json = JsonConvert.SerializeObject(new FoldersResponse(null, Folder.topLevelFolders()), Formatting.None);
			_sh.outputStream.WriteLine(json);
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

		public FoldersResponse(string Error, List<Folder> Folders)
		{
			error = Error;
			folders = Folders;
		}
	}
}
