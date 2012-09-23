using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.DataModel.Model
{
	public enum FileType
	{
        AAC = 1, // Starts with 1 for database compatibility
        MP3 = 2,
        MPC = 3,
        OGG = 4,
        WMA = 5,
        ALAC = 6,
        APE = 7,
        FLAC = 8,
        WV = 9,
		MP4 = 10,
		MKV = 11,
		AVI = 12,
		Unknown = 2147483647 // Int32.MaxValue used for database compatibility
	}

	public static class FileTypeExtensions
	{
		/*public static FileType FileTypeForTagSharpString(this FileType ft, string formatString)
		{
			Console.WriteLine("format string: " + formatString);

            // Lossy codecs
			if (formatString == "MPEG-4 Audio (mp4a)")
				return FileType.AAC;
			else if (formatString == "MPEG Version 1 Audio, Layer 3 VBR" || formatString == "MPEG Version 1 Audio, Layer 3")
				return FileType.MP3;
			else if (formatString == "MusePack Version 7 Audio")
				return FileType.MPC;
			else if (formatString == "Vorbis Version 0 Audio")
				return FileType.OGG;
			else if (formatString == "Microsoft WMA2 Audio" || formatString == "Microsoft Lossless WMA Audio")
				return FileType.WMA;
            // Lossless codecs; reordered slightly to prevent ArgumentOutOfRange exception on substring()
			else if (formatString == "MPEG-4 Audio (alac)")
				return FileType.ALAC;
			else if (formatString == "Flac Audio")
				return FileType.FLAC;
            // These two use substrings because their version numbers constantly increment with each release
			else if (formatString.Contains("WavPack"))
				return FileType.WV;
			else if (formatString.Contains("Monkey's Audio"))
				return FileType.APE;
			else if (formatString.Contains("MPEG-4 Video (avc1)"))
				return FileType.MP4;
            else
				return FileType.Unknown;
		}*/

		public static FileType FileTypeForMimeType(this FileType ft, string mimeType)
		{
			Console.WriteLine("mime type: " + mimeType);

			// Lossy codecs
			if (mimeType == "taglib/m4a" || mimeType == "taglib/aac") 
				return FileType.AAC;
			else if (mimeType == "taglib/mp3")
				return FileType.MP3;
			else if (mimeType == "taglib/mpc")
				return FileType.MPC;
			else if (mimeType == "taglib/oggo")
				return FileType.OGG;
			else if (mimeType == "taglib/wma")
				return FileType.WMA;
			//else if (formatString == "MPEG-4 Audio (alac)")
			//	return FileType.ALAC;
			else if (mimeType == "taglib/flac")
				return FileType.FLAC;
			else if (mimeType == "taglib/wv")
				return FileType.WV;
			else if (mimeType == "taglib/ape")
				return FileType.APE;
			else if (mimeType.Contains("taglib/mp4"))
				return FileType.MP4;
			else if (mimeType.Contains("taglib/mkv"))
				return FileType.MKV;
			else if (mimeType.Contains("taglib/avi"))
				return FileType.AVI;
			else
				return FileType.Unknown;
		}

		public static FileType FileTypeForId(this FileType ft, int id)
		{
			foreach (FileType type in Enum.GetValues(typeof(FileType)))
			{
				if (id == (int)type)
				{
					return type;
				}
			}
			return FileType.Unknown;
		}
	}
}
