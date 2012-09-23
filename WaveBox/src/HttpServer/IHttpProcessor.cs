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
		void WriteJsonHeader();
		void WriteErrorHeader();
		void WriteFileHeader(long contentLength);

		// Body writing methods
		void WriteJson(string json);
		void WriteFile(Stream fs, int startOffset, long length);
	}
}

