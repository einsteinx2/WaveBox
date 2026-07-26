using System;
using System.Text;

namespace WaveBox.Core.Extensions {
    public static class IntExtensions {
        /// <summary>
        /// Return a media-style time string based on this integer
        /// </summary>
        public static string ToTimeString(this int duration) {
            TimeSpan ts = TimeSpan.FromSeconds(duration);

            // With hours present the minutes must always appear (two-digit), even when zero,
            // so 3600 seconds renders as "1:00:00" rather than "1:00"
            if (ts.Hours > 0) {
                return ts.Hours + ":" + ts.ToString("mm") + ":" + ts.ToString("ss");
            }

            if (ts.Minutes > 0) {
                return ts.Minutes + ":" + ts.ToString("ss");
            }

            // Seconds with leading zero
            return ts.ToString("ss");
        }
    }
}
