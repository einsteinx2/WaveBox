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
		Unknown = 2147483647 // Int32.MaxValue used for database compatibility
	}

	public static class FileTypeExtensions
	{
		public static FileType FileTypeForTagSharpString(this FileType ft, string audioFormatString)
		{
            // Lossy codecs
            if (audioFormatString == "MPEG-4 Audio (mp4a)")
				return FileType.AAC;
            else if (audioFormatString == "MPEG Version 1 Audio, Layer 3 VBR" || audioFormatString == "MPEG Version 1 Audio, Layer 3")
				return FileType.MP3;
            else if (audioFormatString == "MusePack Version 7 Audio")
				return FileType.MPC;
            else if (audioFormatString == "Vorbis Version 0 Audio")
				return FileType.OGG;
            else if (audioFormatString == "Microsoft WMA2 Audio" || audioFormatString == "Microsoft Lossless WMA Audio")
				return FileType.WMA;
            // Lossless codecs; reordered slightly to prevent ArgumentOutOfRange exception on substring()
            else if (audioFormatString == "MPEG-4 Audio (alac)")
				return FileType.ALAC;
            else if (audioFormatString == "Flac Audio")
				return FileType.FLAC;
            // These two use substrings because their version numbers constantly increment with each release
            else if (audioFormatString.Substring(0, 7) == "WavPack")
				return FileType.WV;
            else if (audioFormatString.Substring(0, 14) == "Monkey's Audio")
				return FileType.APE;
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
