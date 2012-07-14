using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.ApiHandler
{
	class UriWrapper
	{
		private List<string> UriParts { get; set; }
		private string Uri { get; set; }

		private Dictionary<string, string> parameters;
		public Dictionary<string, string> Parameters
		{
			get
			{
				return parameters;
			}
		}

		public UriWrapper(string uri)
		{
			//Console.WriteLine("uri: {0}", uri);
			Uri = uri;
			ParseParameters();
			UriParts = RemoveEmptyElements(Uri.Split('/'));
		}

		private void ParseParameters()
		{
			if(Uri.Contains('?'))
			{
				parameters = new Dictionary<string,string>();
				// if we split the uri by the question mark, the second part of the split will be the params
				string parametersString = Uri.Split('?')[1];
				string[] splitParams = parametersString.Split(new char[]{'=', '&'});

				for(int i = 0; i <= splitParams.Length - 2; i = i + 2)
				{
					parameters.Add(splitParams[i], splitParams[i + 1]);
				}

				Uri = Uri.Substring(0, Uri.IndexOf('?'));
			}
		}

		public string UriPart(int index)
		{
			if (UriParts.Count > index)
			{
				return UriParts.ElementAt(index);
			}

			return null;
		}

		public string FirstPart()
		{
			return UriPart(0);
		}

		public string LastPart()
		{
			return UriPart(UriParts.Count - 1);
		}

		private List<string> RemoveEmptyElements(string[] input)
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
