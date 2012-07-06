using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaFerry.DataModel.Model
{
	public enum FileType
	{
		MP3 = 0,
		AAC = 1, 
		OGG = 2,
        WMA = 3,
        MPC = 4, 
		FLAC = 5,
		ALAC = 6,
        WV = 7,
		UNKNOWN = -1
	}

	public static class FileTypeExtensions
	{
		public static FileType fileTypeForTagSharpString(this FileType ft, string audioFormatString)
		{
            if (audioFormatString == "MPEG Version 1 Audio, Layer 3 VBR" || audioFormatString == "MPEG Version 1 Audio, Layer 3") return FileType.MP3;
            else if (audioFormatString == "MPEG-4 Audio (mp4a)") return FileType.AAC;
            else if (audioFormatString == "Vorbis Version 0 Audio") return FileType.OGG;
            else if (audioFormatString == "Microsoft WMA2 Audio" || audioFormatString == "Microsoft Lossless WMA Audio") return FileType.WMA;
            else if (audioFormatString == "MusePack Version 7 Audio") return FileType.MPC;
            else if (audioFormatString == "Flac Audio") return FileType.FLAC;
            else if (audioFormatString == "MPEG-4 Audio (alac)") return FileType.ALAC;
            else if (audioFormatString.Substring(0, 7) == "WavPack") return FileType.WV;
            else return FileType.UNKNOWN;
		}

		public static FileType fileTypeForId(this FileType ft, int id)
		{
			foreach (FileType type in Enum.GetValues(typeof(FileType)))
			{
				if (id == (int)type)
				{
					return type;
				}
			}
			return FileType.UNKNOWN;
		}


	}
}
