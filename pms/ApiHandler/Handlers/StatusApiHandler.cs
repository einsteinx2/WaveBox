using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;
using MediaFerry.DataModel.Singletons;
using Newtonsoft.Json;

namespace MediaFerry.ApiHandler.Handlers
{
	class StatusApiHandler : IApiHandler
	{
		private HttpProcessor _sh;
		private UriWrapper _uriW;

		public StatusApiHandler(UriWrapper uriW, HttpProcessor sh)
		{
			_sh = sh;
			_uriW = uriW;
		}

		public void process()
		{
			Console.WriteLine("Status: ok!");
			string json = JsonConvert.SerializeObject(new StatusResponse(null, 1), Formatting.None);
			_sh.outputStream.Write(json);
		}
	}

	class StatusResponse
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

		private double _version;
		public double version
		{
			get
			{
				return _version;
			}
			set
			{
				_version = value;
			}
		}

		public StatusResponse(string Error, double Version)
		{
			error = Error;
			version = Version;
		}
	}
}
