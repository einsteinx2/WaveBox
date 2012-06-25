using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pms.DataModel.Model
{
	public enum FileType
	{
		MP3 = 0,
        AAC = 1, 
        OGG = 2,
        FLAC16 = 3,
        FLAC24 = 4,
        WAV = 5,
        AIFF = 6,
        ALAC = 7,
        UNKNOWN = -1
	}

    public static class FileTypeExtensions
    {
        public static FileType fileTypeForTagSharpString(this FileType ft, string audioFormatString)
        {
            if (audioFormatString == "MPEG Layer III" || audioFormatString == "MPEG Version 1 Audio, Layer 3") return FileType.MP3;
            else if (audioFormatString == "Advanced Audio Coding" || audioFormatString == "AAC") return FileType.AAC;
            else if (audioFormatString == "Ogg Vorbis" || audioFormatString == "OGG") return FileType.OGG;
            else if (audioFormatString == "Free Lossless Audio Codec" || audioFormatString == "FLAC 16 bits") return FileType.FLAC16;
            else if (audioFormatString == "Free Lossless Audio Codec" || audioFormatString == "FLAC 24 bits") return FileType.FLAC24;
            else if (audioFormatString == "Waveform Audio File Format" || audioFormatString == "WAV") return FileType.WAV;
            else if (audioFormatString == "Audio Interchange File Format" || audioFormatString == "AIFF") return FileType.AIFF;
            else if (audioFormatString == "Apple Lossless Audio Codec" || audioFormatString == "Apple Lossless") return FileType.ALAC;
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
