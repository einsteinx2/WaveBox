using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using WaveBox.Transcoding;

namespace WaveBox.Service.Services.Http
{
	public interface IHttpProcessor
	{
		// A dictionary of string keys and values representing the headers received from the client
		Hashtable HttpHeaders { get; set; }

		ITranscoder Transcoder { get; set; }

		// Header writing methods
		void WriteErrorHeader();
		void WriteSuccessHeader(long contentLength, string mimeType, IDictionary<string, string> customHeaders, DateTime lastModified, bool isPartial = false);

		// Body writing methods
		void WriteNotModifiedHeader();
		void WriteJson(string json);
		void WriteText(string text, string mimeType);

		void WriteFile(Stream fs, int startOffset, long length, string mimeType, IDictionary<string, string> customHeaders, bool isSendContentLength, DateTime? lastModified, long? limitToBytes = null);
	}
}
