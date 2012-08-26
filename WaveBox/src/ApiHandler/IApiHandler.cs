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
		public static bool IsTrue(this IApiHandler handler, string boolString)
		{
			try
			{
				if (boolString == null)
				{
					return false;
				}
			  	
				// Lowercase and trim whitespace
				boolString = boolString.ToLower();
				boolString = boolString.Trim();

				if (boolString.Length > 0)
				{
					if (boolString[0] == 't' || boolString[0] == '1')
					{
						return true;
					}
				}

				return false;
			}
			catch
			{
				return false;
			}
		}
	}
}
