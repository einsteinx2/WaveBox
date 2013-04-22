using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.ApiHandler
{
	// This interface is implemented by all API handlers, to ensure that the Process() method is implemented
	interface IApiHandler
	{
		void Process();
	}

	static class ApiHandlerExtension
	{
		/// <summary>
		/// Determine if a string is meant to indicate true, return false if none detected
		/// </summary>
		public static bool IsTrue(this IApiHandler handler, string boolString)
		{
			try
			{
				// Null string -> false
				if (boolString == null)
				{
					return false;
				}

				// Lowercase and trim whitespace
				boolString = boolString.ToLower();
				boolString = boolString.Trim();

				if (boolString.Length > 0)
				{
					// t or 1 -> true
					if (boolString[0] == 't' || boolString[0] == '1')
					{
						return true;
					}
				}

				// Anything else, false
				return false;
			}
			catch
			{
				// Exception, false
				return false;
			}
		}
	}
}
