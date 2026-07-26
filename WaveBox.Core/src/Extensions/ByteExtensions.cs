using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WaveBox.Core.Extensions {
    public static class ByteExtensions {
        /// <summary>
        /// Generates a MD5 sum of a given byte array, as lowercase hex
        /// <summary>
        public static string MD5(this byte[] input) {
            byte[] hash = System.Security.Cryptography.MD5.HashData(input);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
