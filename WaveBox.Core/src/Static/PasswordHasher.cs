using System;
using System.Text;

namespace WaveBox.Core.Static {
    /// <summary>
    /// Password storage, wrapping bcrypt so the library stays out of the call sites.
    ///
    /// A bcrypt hash is self describing -- "$2a$&lt;cost&gt;$&lt;22 char salt&gt;&lt;31 char hash&gt;" -- so the salt
    /// and work factor travel with it and no separate salt column is needed.
    /// </summary>
    public static class PasswordHasher {
        /// <summary>
        /// bcrypt only consumes the first 72 bytes of a password and silently ignores the rest, so
        /// longer passwords are rejected outright rather than quietly truncated.
        /// </summary>
        public const int MaxPasswordBytes = 72;

        /// <summary>
        /// Work factor, as a power of two. OWASP's guidance is a minimum of 10, set as high as
        /// verification performance allows while staying under a second on the slowest target.
        ///
        /// Measured on an M-series Mac: cost 10 ~115ms, 11 ~140ms, 12 ~279ms, 13 ~552ms. Pi-class
        /// linux-arm64 hardware runs several times slower, which would put 12 over the one second
        /// mark there, so 11 it is. Subsonic clients re-send credentials on every request, and
        /// SubsonicAuth only caches a verification for ten minutes, so this sets a floor on
        /// request latency for clients that don't keep connections alive.
        ///
        /// Tests dial this down to bcrypt's minimum, since they hash on nearly every fixture.
        /// </summary>
        internal static int WorkFactor = 11;

        /// <summary>
        /// Hashes a password for storage, or returns null if it is unusable. bcrypt generates its
        /// own salt, so calling this twice with the same password yields two different hashes.
        /// </summary>
        public static string Hash(string password) {
            if (!IsAcceptable(password)) {
                return null;
            }

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        /// <summary>
        /// Verifies a password against a stored hash, in time independent of how much of the hash
        /// matched. Returns false for anything malformed rather than throwing.
        /// </summary>
        public static bool Verify(string password, string storedHash) {
            if (String.IsNullOrEmpty(password) || String.IsNullOrEmpty(storedHash)) {
                return false;
            }

            try {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            } catch (BCrypt.Net.SaltParseException) {
                // Not a bcrypt hash at all
                return false;
            }
        }

        /// <summary>
        /// Whether a password can be stored, i.e. non-empty and within bcrypt's input limit.
        /// </summary>
        public static bool IsAcceptable(string password) {
            return !String.IsNullOrEmpty(password)
                && Encoding.UTF8.GetByteCount(password) <= MaxPasswordBytes;
        }
    }
}
