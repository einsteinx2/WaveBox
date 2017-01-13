using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace WaveBox.Core.Extensions {
    public static class StreamExtensions {
        /// <summary>
        /// Generates a MD5 sum of a given input Stream
        /// Thanks: http://msdn.microsoft.com/en-us/library/s02tk69a.aspx
        /// <summary>
        public static string MD5(this Stream input) {
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

        /// <summary>
        /// Directly read a line from an input stream (typically for HTTP)
        /// </summary>
        public static string ReadLine(this Stream input) {
            int next_char = 0;
            int readTries = 0;
            string data = "";

            // Loop until newline
            while (true) {
                // Read character
                next_char = input.ReadByte();

                // Check for valid character
                if (next_char == -1) {
                    if (readTries >= 29) {
                        throw new IOException("ReadByte timed out", null);
                    }
                    readTries++;
                    Thread.Sleep(1);
                    continue;
                } else {
                    readTries = 0;
                }

                // Skip carriage returns
                if (next_char == '\r') {
                    continue;
                }

                // Stop reading on newline
                if (next_char == '\n') {
                    break;
                }

                // Parse valid characters
                data += Convert.ToChar(next_char);
            }

            // Return the line
            return data;
        }
    }
}
