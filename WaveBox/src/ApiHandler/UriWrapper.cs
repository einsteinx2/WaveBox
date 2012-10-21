using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;

namespace WaveBox.ApiHandler
{
	public class UriWrapper
	{		
		//private static Logger logger = LogManager.GetCurrentClassLogger();

		public List<string> UriParts { get; set; }
		public string UriString { get; set; }
		public Dictionary<string, string> Parameters { get; set; }

		public string FirstPart { get { return UriPart(0); } }
		public string LastPart { get { return UriPart(UriParts.Count - 1); } }

		public bool IsApiCall { get { return FirstPart == "api"; } }
		public string Action { get { return IsApiCall ? UriPart(1).ToLower() : null; } }

		/// <summary>
		/// Constructor for UriWrapper, takes in a URI string and enables methods to parse its pieces
		/// </summary>
		public UriWrapper(string uriString)
		{
			// Store the original URI in a string
			UriString = uriString;

			// Parse parameters in the URI
			ParseParameters();

			// Store the parts of the URI in a List of strings
			UriParts = RemoveEmptyElements(UriString.Split('/'));
		}

		/// <summary>
		/// Parse parameters into a Dictionary from a URI
		/// </summary>
		private void ParseParameters()
		{
			// Make sure the URI contains parameters
			if(UriString.Contains('?'))
			{
				// Initialize a dictionary
				Parameters = new Dictionary<string,string>();

				// if we split the uri by the question mark, the second part of the split will be the params
				string parametersString = UriString.Split('?')[1];
				string[] splitParams = parametersString.Split(new char[]{'=', '&'});

				// Add parameters to the dictionary as we parse the parameters array
				for(int i = 0; i <= splitParams.Length - 2; i = i + 2)
				{
					Parameters.Add(splitParams[i], splitParams[i + 1]);
				}

				// Store the URI before parameters in the UriString property
				UriString = UriString.Substring(0, UriString.IndexOf('?'));
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
		/// Purge the empty elements in an array of strings, returning a list of strings
		/// </summary>
		private List<string> RemoveEmptyElements(string[] input)
		{
			List<string> result = new List<string>();

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
