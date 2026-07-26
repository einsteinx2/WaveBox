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
        /// Generates a MD5 sum of a given input Stream, as lowercase hex
        /// <summary>
        public static string MD5(this Stream input) {
            byte[] hash = System.Security.Cryptography.MD5.HashData(input);
            return Convert.ToHexString(hash).ToLowerInvariant();
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
