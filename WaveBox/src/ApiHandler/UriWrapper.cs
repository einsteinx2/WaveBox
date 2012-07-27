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
		public Dictionary<string, string> Parameters { get; set; }

		/// <summary>
		/// Constructor for UriWrapper, takes in a URI string and enables methods to parse its pieces
		/// </summary>
		public UriWrapper(string uri)
		{
			// Store the original URI in a string
			Uri = uri;

			// Parse parameters in the URI
			ParseParameters();

			// Store the parts of the URI in a List of strings
			UriParts = RemoveEmptyElements(Uri.Split('/'));
		}

		/// <summary>
		/// Parse parameters into a Dictionary from a URI
		/// </summary>
		private void ParseParameters()
		{
			// Make sure the URI contains parameters
			if(Uri.Contains('?'))
			{
				// Initialize a dictionary
				Parameters = new Dictionary<string,string>();

				// if we split the uri by the question mark, the second part of the split will be the params
				string parametersString = Uri.Split('?')[1];
				string[] splitParams = parametersString.Split(new char[]{'=', '&'});

				// Add parameters to the dictionary as we parse the parameters array
				for(int i = 0; i <= splitParams.Length - 2; i = i + 2)
				{
					Parameters.Add(splitParams[i], splitParams[i + 1]);
				}

				// Store the URI before parameters in the Uri property
				Uri = Uri.Substring(0, Uri.IndexOf('?'));
			}
		}

		/// <summary>
		/// Return the element at a given index of the URI
		/// </summary>
		public string UriPart(int index)
		{
			// Make sure the URI's part count is greater than the index
			if (UriParts.Count > index) 
			{
				return UriParts.ElementAt(index);
			}

			// Return null if the index was out of range
			return null;
		}

		/// <summary>
		/// Return the first part of the URI
		/// </summary>
		public string FirstPart()
		{
			return UriPart(0);
		}

		/// <summary>
		/// Return the last part of the URI
		/// </summary>
		public string LastPart()
		{
			return UriPart(UriParts.Count - 1);
		}

		/// <summary>
		/// Purge the empty elements in an array of strings, returning a list of strings
		/// </summary>
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
