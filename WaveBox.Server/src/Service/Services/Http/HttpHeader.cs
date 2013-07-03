using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WaveBox.Service.Services.Http
{
	public class HttpHeader
	{
		public long ContentLength { get; set; }
		public HttpStatusCode StatusCode { get; set; }
		public string ContentType { get; set; }

		private bool isBinary = false;
		public bool IsBinary
		{
			get { return isBinary; }
		}

		public enum HttpStatusCode
		{
			OK = 0, 
			NOTFOUND = 1,
			PARTIALCONTENT = 2,
			NOTMODIFIED = 3
		}

		public enum HttpContentType
		{
			AUDIOMP3 = 0,
			AUDIOMP4 = 1,
			AUDIOOGG = 2,
			AUDIOWEBM = 3,
			AUDIOWAV = 4,
			VIDEOWEBM = 5,
			VIDEOMP4 = 6,
			VIDEOOGG = 7,
			HTML = 8,
			CSS = 9,
			JAVASCRIPT = 10,
			PLAINTEXT = 11, 
			UNKNOWN = 12
		}

		private static string[] HttpContentTypeStrings = { "audio/mpeg", "audio/mp4", "audio/ogg", "audio/webm", "audio/wav", "video/webm", "video/mp4", "video/ogg", "text/html", "text/css", "text/javascript", "text/plain", "octet-stream" };

		public static HttpContentType ContentTypeForExtension(string extension)
		{
			string ext = extension.ToLower();

			switch (ext)
			{
				case ".mp3":
					return HttpContentType.AUDIOMP3;
				case ".m4a":
					return HttpContentType.AUDIOMP4;
				case ".ogg":
				case ".oga":
					return HttpContentType.AUDIOOGG;
				case ".webma":
					return HttpContentType.AUDIOWEBM;
				case ".wav":
					return HttpContentType.AUDIOWAV;
				case ".mp4":
				case ".m4v":
					return HttpContentType.VIDEOMP4;
				case ".ogv":
					return HttpContentType.VIDEOOGG;
				case ".webm":
				case ".webmv":
					return HttpContentType.VIDEOWEBM;
				case ".html":
				case ".htm":
					return HttpContentType.HTML;
				case ".css":
					return HttpContentType.CSS;
				case ".js":
					return HttpContentType.JAVASCRIPT;
				case ".txt":
					return HttpContentType.PLAINTEXT;
				default:
					return HttpContentType.UNKNOWN;
			}
		}

		public static string MimeTypeForExtension(string extension)
		{
			return HttpContentTypeStrings[(int)ContentTypeForExtension(extension)];
		}

		public HttpHeader()
		{
			StatusCode = HttpStatusCode.OK;
			ContentType = "text/html";
			ContentLength = 0;
		}

		public HttpHeader(HttpStatusCode s, HttpContentType cType, long cLen)
		{
			StatusCode = s;
			ContentType = HttpContentTypeStrings[(int)cType];
			ContentLength = cLen;
			isBinary = ContentTypeIsBinary(cType);
		}

		public void WriteHeader(StreamWriter outputStream)
		{
			// write the headers
			outputStream.Write(HeaderStringWithResponseCode(StatusCode) + "\r\n");
			outputStream.Write("Content-Length: " + ContentLength + "\r\n");
			if (ContentType != "")
			{
				outputStream.Write("Content-Type: " + ContentType + "\r\n");
			}
			if (IsBinary)
			{
				outputStream.Write("Accept-Ranges: bytes\r\n");
			}

			// write the last newline signifying the end of the headers
			outputStream.Write("\r\n");
		}

		public string HeaderStringWithResponseCode(HttpStatusCode c)
		{
			switch (c)
			{
				case HttpStatusCode.OK: return "HTTP/1.0 200 OK";
				case HttpStatusCode.NOTFOUND: return "HTTP/1.0 404 Not Found";
				case HttpStatusCode.PARTIALCONTENT: return "HTTP/1.1 206 Partial Content";
				case HttpStatusCode.NOTMODIFIED: return "HTTP/1.1 304 Not Modified";
				default: return null;
			}
		}

		public static bool ContentTypeIsBinary(HttpHeader.HttpContentType theType)
		{
			HttpHeader.HttpContentType[] binaryTypes = new HttpHeader.HttpContentType[] 
			{ 
				HttpHeader.HttpContentType.AUDIOMP3,
				HttpHeader.HttpContentType.AUDIOMP4,
				HttpHeader.HttpContentType.AUDIOOGG,
				HttpHeader.HttpContentType.AUDIOWAV,
				HttpHeader.HttpContentType.AUDIOWEBM,
				HttpHeader.HttpContentType.VIDEOMP4,
				HttpHeader.HttpContentType.VIDEOOGG,
				HttpHeader.HttpContentType.VIDEOWEBM,
			};

			foreach (HttpHeader.HttpContentType type in binaryTypes)
			{
				if (theType == type)
				{
					return true;
				}
			}

			return false;
		}
	}
}
