using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.HttpServer
{
	class WaveBoxHttpHeader
	{
		public long ContentLength { get; set; }
		public HttpStatusCode StatusCode { get; set; }
		public string ContentType { get; set; }

		public enum HttpStatusCode
		{
			OK = 0, 
			NOTFOUND = 1
		}

		public WaveBoxHttpHeader()
		{
			StatusCode = HttpStatusCode.OK;
			ContentType = "text/html";
			ContentLength = 0;
		}

		public WaveBoxHttpHeader(HttpStatusCode s, string cType, long cLen)
		{
			StatusCode = s;
			ContentType = cType;
			ContentLength = cLen;
		}

		public void WriteHeader(HttpProcessor hp)
		{
			// write the headers
			hp.OutputStream.Write(HeaderStringWithResponseCode(StatusCode) + "\r\n");
			hp.OutputStream.Write("Content-Length: " + ContentLength + "\r\n");
			if(ContentType != "")
				hp.OutputStream.Write("Content-Type: " + ContentType + "\r\n");

			// write the last newline signifying the end of the headers
			hp.OutputStream.Write("\r\n");
		}

		public string HeaderStringWithResponseCode(HttpStatusCode c)
		{
			switch (c)
			{
				case HttpStatusCode.OK: return "HTTP/1.0 200 OK";
				case HttpStatusCode.NOTFOUND: return "HTTP/1.0 404 File not found";
				default: return null;
			}
		}
	}
}
