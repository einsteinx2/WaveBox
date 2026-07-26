using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WaveBox.Core.Extensions {
    public static class StringExtensions {
        /// <summary>
        /// Determine if a string is meant to indicate true, return false if none detected
        /// </summary>
        public static bool IsTrue(this string boolString) {
            try {
                // Null string -> false
                if (boolString == null) {
                    return false;
                }

                // Lowercase and trim whitespace
                boolString = boolString.ToLower();
                boolString = boolString.Trim();

                if (boolString.Length > 0) {
                    // t or 1 -> true
                    if (boolString[0] == 't' || boolString[0] == '1') {
                        return true;
                    }
                }

                // Anything else, false
                return false;
            } catch {
                // Exception, false
                return false;
            }
        }

        /// <summary>
        /// Generates a MD5 sum of a given string, as lowercase hex
        /// <summary>
        public static string MD5(this string sumthis) {
            if (sumthis == "" || sumthis == null) {
                return "";
            }

            byte[] hash = System.Security.Cryptography.MD5.HashData(Encoding.ASCII.GetBytes(sumthis));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Returns an integer representation of a month string
        /// </summary>
        public static int MonthForAbbreviation(this string abb) {
            switch (abb.ToLower()) {
            case "jan":
                return 1;
            case "feb":
                return 2;
            case "mar":
                return 3;
            case "apr":
                return 4;
            case "may":
                return 5;
            case "jun":
                return 6;
            case "jul":
                return 7;
            case "aug":
                return 8;
            case "sep":
                return 9;
            case "oct":
                return 10;
            case "nov":
                return 11;
            case "dec":
                return 12;
            default:
                return 0;
            }
        }

        /// <summary>
        /// Generates a SHA1 sum of a given string, as lowercase hex
        /// </summary>
        public static string SHA1(this string sumthis) {
            if (sumthis == "" || sumthis == null) {
                return "";
            }

            byte[] hash = System.Security.Cryptography.SHA1.HashData(Encoding.ASCII.GetBytes(sumthis));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Remove UTF8 byte order mark from a string
        /// </summary>
        public static string RemoveByteOrderMark(this string s) {
            string byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

            // Ordinal comparison is required: under culture-sensitive comparison U+FEFF is a
            // zero-weight character, so every string would "start with" the BOM and lose its
            // first character instead
            if (s.StartsWith(byteOrderMarkUtf8, StringComparison.Ordinal)) {
                return s.Remove(0, byteOrderMarkUtf8.Length);
            }

            return s;
        }
    }
}
