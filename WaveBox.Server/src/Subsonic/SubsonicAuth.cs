using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Subsonic {
    // Stateless per-request Subsonic authentication.
    //
    // Supported mechanisms:
    //   - apiKey=...            (OpenSubsonic apiKeyAuthentication extension)
    //   - u=user&p=password     (plaintext or p=enc:HEX), verified against the PBKDF2 hash
    // Classic token auth (t=md5(password+salt)) is impossible with hashed-only password
    // storage and returns error 42 so clients fall back or surface a clear message.
    //
    // Because PBKDF2 verification is deliberately expensive and Subsonic clients send
    // credentials on every request (including every seek within a stream), successful u/p
    // verifications are cached for a sliding TTL keyed by a SHA256 of the presented password.
    public class SubsonicAuth {
        private class VerifiedAuth {
            public byte[] PasswordSha256;
            public DateTime Expires;
        }

        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

        private readonly ConcurrentDictionary<string, VerifiedAuth> verified = new ConcurrentDictionary<string, VerifiedAuth>(StringComparer.Ordinal);

        // Returns the authenticated user (a per-request copy carrying a synthesized in-memory
        // session), or null with a populated error
        public User Authenticate(SubsonicRequest req, out SubsonicError error) {
            error = null;

            string apiKey = req.Get("apiKey");
            string username = req.Get("u");
            string password = req.Get("p");
            string token = req.Get("t");
            string salt = req.Get("s");

            if (apiKey != null) {
                if (username != null || password != null || token != null || salt != null) {
                    error = new SubsonicError { Code = SubsonicError.ConflictingMechanisms, Message = "Multiple conflicting authentication mechanisms provided" };
                    return null;
                }

                User keyUser = this.UserForApiKey(apiKey);
                if (keyUser == null) {
                    error = new SubsonicError { Code = SubsonicError.InvalidApiKey, Message = "Invalid API key" };
                    return null;
                }

                return this.Authenticated(keyUser, req);
            }

            if (token != null || salt != null) {
                error = new SubsonicError { Code = SubsonicError.MechanismNotSupported, Message = "Token authentication is not supported; use an API key or password authentication" };
                return null;
            }

            if (username == null || password == null) {
                error = new SubsonicError { Code = SubsonicError.MissingParameter, Message = "Required authentication parameter is missing" };
                return null;
            }

            // Hex-encoded password variant: p=enc:48656c6c6f
            if (password.StartsWith("enc:", StringComparison.OrdinalIgnoreCase)) {
                try {
                    password = Encoding.UTF8.GetString(Convert.FromHexString(password.Substring(4)));
                } catch (FormatException) {
                    error = new SubsonicError { Code = SubsonicError.WrongCredentials, Message = "Wrong username or password" };
                    return null;
                }
            }

            User user = Injection.Get<IUserRepository>().UserForName(username);
            if (user == null || user.UserId == null) {
                error = new SubsonicError { Code = SubsonicError.WrongCredentials, Message = "Wrong username or password" };
                return null;
            }

            byte[] presented = SHA256.HashData(Encoding.UTF8.GetBytes(password));

            VerifiedAuth cached;
            bool cacheHit = this.verified.TryGetValue(username, out cached)
                && cached.Expires > DateTime.UtcNow
                && CryptographicOperations.FixedTimeEquals(cached.PasswordSha256, presented);

            if (!cacheHit && !user.Authenticate(password)) {
                error = new SubsonicError { Code = SubsonicError.WrongCredentials, Message = "Wrong username or password" };
                return null;
            }

            // Cache the successful verification with a sliding expiry
            this.verified[username] = new VerifiedAuth { PasswordSha256 = presented, Expires = DateTime.UtcNow + CacheTtl };

            return this.Authenticated(user, req);
        }

        // Drop a user's cached verification (call after password change, user update, or delete)
        public void Evict(string username) {
            if (username != null) {
                VerifiedAuth removed;
                this.verified.TryRemove(username, out removed);
            }
        }

        private User UserForApiKey(string apiKey) {
            if (String.IsNullOrEmpty(apiKey)) {
                return null;
            }

            byte[] presented = Encoding.UTF8.GetBytes(apiKey);
            foreach (User candidate in Injection.Get<IUserRepository>().AllUsers()) {
                if (String.IsNullOrEmpty(candidate.ApiKey)) {
                    continue;
                }
                byte[] stored = Encoding.UTF8.GetBytes(candidate.ApiKey);
                if (CryptographicOperations.FixedTimeEquals(stored, presented)) {
                    return candidate;
                }
            }

            return null;
        }

        // Repository users are shared cache instances; hand each request its own copy so the
        // synthesized session (and its client name) can't leak across concurrent requests
        private User Authenticated(User user, SubsonicRequest req) {
            return new User {
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role,
                LastfmSession = user.LastfmSession,
                CreateTime = user.CreateTime,
                DeleteTime = user.DeleteTime,
                // In-memory only, never persisted: NowPlayingService reads CurrentSession.ClientName
                CurrentSession = new Session {
                    UserId = user.UserId,
                    ClientName = req.ClientName
                }
            };
        }
    }
}
