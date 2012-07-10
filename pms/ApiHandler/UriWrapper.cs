using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaFerry.ApiHandler
{
	class UriWrapper
	{
		private List<string> _uriParts;
		private string _uri;

		private Dictionary<string, string> _parameters;
		public Dictionary<string, string> Parameters
		{
			get
			{
				return _parameters;
			}
		}

		public UriWrapper(string uri)
		{
			//Console.WriteLine("uri: {0}", uri);
			_uri = uri;
			_uriParts = _removeEmptyElements(uri.Split(new char[]{'/', '?'}));
			_parseParameters();
		}

		private void _parseParameters()
		{
			if(_uri.Contains('?'))
			{
				_parameters = new Dictionary<string,string>();
				// if we split the uri by the question mark, the second part of the split will be the params
				string parameters = _uri.Split('?')[1];
				string[] splitParams = parameters.Split('=');

				for(int i = 0; i <= splitParams.Length - 2; i = i + 2)
				{
					_parameters.Add(splitParams[i], splitParams[i + 1]);
				}
			}
		}

		public string getUriPart(int index)
		{
			if (_uriParts.Count > index)
			{
				return _uriParts.ElementAt(index);
			}

			return null;
		}

		public string getFirstPart()
		{
			return getUriPart(0);
		}

		public string getLastPart()
		{
			return getUriPart(_uriParts.Count - 1);
		}

		private List<string> _removeEmptyElements(string[] input)
		{
			var result = new List<string>();

			foreach (string s in input)
			{
				if (s != null && s != "")
				{
					result.Add(s);
				}
			}

			return result;
		}

		
	}
}
