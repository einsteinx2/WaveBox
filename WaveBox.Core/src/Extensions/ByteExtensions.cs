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
        /// Generates a MD5 sum of a given byte array
        /// Thanks: http://msdn.microsoft.com/en-us/library/s02tk69a.aspx
        /// <summary>
        public static string MD5(this byte[] input) {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5.ComputeHash(input);

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++) {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }
}
