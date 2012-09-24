using System;
using System.IO;
using System.Collections;
using WaveBox.Transcoding;

namespace WaveBox
{
	public interface IHttpProcessor
	{
		// A dictionary of string keys and values representing the 
		// headers received from the client
		Hashtable HttpHeaders { get; set; }

		ITranscoder Transcoder { get; set; }

		// Header writing methods
		void WriteErrorHeader();
		void WriteSuccessHeader(long contentLength, string mimeType);

		// Body writing methods
		void WriteJson(string json);
		void WriteText(string text, string mimeType);
		void WriteFile(Stream fs, int startOffset, long length, string mimeType);
	}
}

