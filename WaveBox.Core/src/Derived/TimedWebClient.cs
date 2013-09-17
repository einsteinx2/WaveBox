using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WaveBox.Core.Derived
{
	public class TimedWebClient : WebClient
	{
		private int timeout;

		// Used to perform a web request with a timeout in milliseconds
		public TimedWebClient(int timeout)
		{
			this.timeout = timeout;
		}

		protected override WebRequest GetWebRequest(Uri uri)
		{
			WebRequest request = base.GetWebRequest(uri);

			// Set timeout
			request.Timeout = this.timeout;
			return request;
		}
	}
}
