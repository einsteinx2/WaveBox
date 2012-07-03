using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bend.Util
{
	class PmsHttpHeader
	{
		private long contentLength;
		public long ContentLength
		{
			get
			{
				return contentLength;
			}

			set
			{
				contentLength = value;
			}
		}

		private HttpStatusCode statusCode;
		public HttpStatusCode StatusCode
		{
			get
			{
				return statusCode;
			}

			set
			{
				statusCode = value;
			}
		}

		private string contentType;
		public string ContentType
		{
			get
			{
				return contentType;
			}

			set
			{
				contentType = value;
			}
		}

		public enum HttpStatusCode
		{
			OK = 0, 
			NOTFOUND = 1
		}

		public PmsHttpHeader()
		{
			StatusCode = HttpStatusCode.OK;
			ContentType = "text/html";
			ContentLength = 0;
		}

		public PmsHttpHeader(HttpStatusCode s, string cType, long cLen)
		{
			StatusCode = s;
			ContentType = cType;
			ContentLength = cLen;
		}

		public void writeHeader(HttpProcessor hp)
		{
			// write the headers
			hp.outputStream.Write(HeaderStringWithResponseCode(StatusCode) + "\r\n");
			hp.outputStream.Write("Content-Length: " + ContentLength + "\r\n");
			if(ContentType != "")
				hp.outputStream.Write("Content-Type: " + ContentType + "\r\n");

			// write the last newline signifying the end of the headers
			hp.outputStream.Write("\r\n");
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
