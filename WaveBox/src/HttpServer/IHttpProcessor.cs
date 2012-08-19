using System;
using System.IO;
using System.Collections;

namespace WaveBox
{
	public interface IHttpProcessor
	{
		Hashtable HttpHeaders { get; set; }
		void WriteJsonHeader();
		void WriteErrorHeader();
		void WriteFileHeader(long contentLength);
		void WriteJson(string json);
		void WriteFile(Stream fs, int startOffset, long length);
	}
}

