using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.Core.Model {
    public enum FileType {
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
}
