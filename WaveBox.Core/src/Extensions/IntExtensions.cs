using System;
using System.Text;

namespace WaveBox.Core.Extensions {
    public static class IntExtensions {
        /// <summary>
        /// Return a media-style time string based on this integer
        /// </summary>
        public static string ToTimeString(this int duration) {
            TimeSpan ts = TimeSpan.FromSeconds(duration);

            string time = "";

            // Hours
            if (ts.Hours > 0) {
                time += ts.Hours + ":";
            }

            // Minutes
            if (ts.Minutes > 0) {
                time += ts.Minutes + ":";
            }

            // Seconds with leading zero and return
            time += ts.ToString("ss");

            return time;
        }
    }
}
