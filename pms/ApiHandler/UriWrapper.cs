using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaFerry.ApiHandler
{
	class UriWrapper
	{
		List<string> _uriParts;

		public UriWrapper(string uri)
		{
			//Console.WriteLine("uri: {0}", uri);
			_uriParts = _removeEmptyElements(uri.Split(new char[]{'/', '?'}));
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
