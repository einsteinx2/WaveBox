using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.DataModel.Model
{
	public enum FileType
	{
        // Alphabetized, lossy first, then lossless, then unknown
        AAC = 0,
        MP3 = 1,
        MPC = 2,
        OGG = 3,
        WMA = 4,
        ALAC = 5,
        APE = 6,
        FLAC = 7,
        WV = 8,
		UNKNOWN = -1
	}

	public static class FileTypeExtensions
	{
		public static FileType FileTypeForTagSharpString(this FileType ft, string audioFormatString)
		{
            // Lossy codecs
            if (audioFormatString == "MPEG-4 Audio (mp4a)") return FileType.AAC;
            else if (audioFormatString == "MPEG Version 1 Audio, Layer 3 VBR" || audioFormatString == "MPEG Version 1 Audio, Layer 3") return FileType.MP3;
            else if (audioFormatString == "MusePack Version 7 Audio") return FileType.MPC;
            else if (audioFormatString == "Vorbis Version 0 Audio") return FileType.OGG;
            else if (audioFormatString == "Microsoft WMA2 Audio" || audioFormatString == "Microsoft Lossless WMA Audio") return FileType.WMA;
            // Lossless codecs; reordered slightly to prevent ArgumentOutOfRange exception on substring()
            else if (audioFormatString == "MPEG-4 Audio (alac)") return FileType.ALAC;
            else if (audioFormatString == "Flac Audio") return FileType.FLAC;
            // These two use substrings because their version numbers constantly increment with each release
            else if (audioFormatString.Substring(0, 7) == "WavPack") return FileType.WV;
            else if (audioFormatString.Substring(0, 14) == "Monkey's Audio") return FileType.APE;

            else return FileType.UNKNOWN;
		}

		public static FileType FileTypeForId(this FileType ft, long id)
		{
			foreach (FileType type in Enum.GetValues(typeof(FileType)))
			{
				if (id == (long)type)
				{
					return type;
				}
			}
			return FileType.UNKNOWN;
		}
	}
}
