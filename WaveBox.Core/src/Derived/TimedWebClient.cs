using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WaveBox.Core.Derived
{
	public class TimedWebClient : WebClient, IWebClient
	{
		protected int timeout;

		// Used to perform a web request with a timeout in milliseconds
		public TimedWebClient(int timeout)
		{
			this.timeout = timeout;

			// Web request optimizations
			ServicePointManager.DefaultConnectionLimit = 128;
			ServicePointManager.Expect100Continue = false;
		}

		protected override WebRequest GetWebRequest(Uri uri)
		{
			WebRequest request = base.GetWebRequest(uri);

			// Disable proxy lookup
			request.Proxy = null;

			// Set timeout
			request.Timeout = this.timeout;
			return request;
		}
	}
}
