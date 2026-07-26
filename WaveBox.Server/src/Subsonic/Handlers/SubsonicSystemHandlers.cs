using System;
using System.Collections.Generic;
using WaveBox.Api;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Subsonic.Handlers {
    public static class SubsonicSystemHandlers {
        public static void Ping(SubsonicRequest req, HttpContextProcessor processor, User user) {
            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }

        public static void GetLicense(SubsonicRequest req, HttpContextProcessor processor, User user) {
            SubsonicResponseBody body = SubsonicWriter.Body();
            // WaveBox is free software; report a perpetually valid license
            body.License = new SubsonicLicense {
                Valid = true,
                LicenseExpires = "2099-12-31T23:59:59Z"
            };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetOpenSubsonicExtensions(SubsonicRequest req, HttpContextProcessor processor, User user) {
            SubsonicResponseBody body = SubsonicWriter.Body();
            body.OpenSubsonicExtensions = new List<SubsonicExtension> {
                new SubsonicExtension { Name = "apiKeyAuthentication", Versions = new List<int> { 1 } },
                new SubsonicExtension { Name = "formPost", Versions = new List<int> { 1 } }
            };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void TokenInfo(SubsonicRequest req, HttpContextProcessor processor, User user) {
            // Only meaningful for API key auth: reports which user the key belongs to
            if (req.Get("apiKey") == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "tokenInfo requires API key authentication");
                return;
            }

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.TokenInfo = new SubsonicTokenInfo { Username = user.UserName };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetScanStatus(SubsonicRequest req, HttpContextProcessor processor, User user) {
            SubsonicResponseBody body = SubsonicWriter.Body();
            body.ScanStatus = new SubsonicScanStatus {
                Scanning = false,
                Count = Injection.Get<ISongRepository>().CountSongs()
            };
            SubsonicWriter.Write(req, processor, body);
        }

        // WaveBox has no last.fm-style artist metadata; an empty artistInfo is valid per the
        // schema (all fields optional) and keeps clients that poll this endpoint happy
        public static void GetArtistInfo(SubsonicRequest req, HttpContextProcessor processor, User user) {
            SubsonicResponseBody body = SubsonicWriter.Body();
            body.ArtistInfo = new SubsonicArtistInfo();
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetArtistInfo2(SubsonicRequest req, HttpContextProcessor processor, User user) {
            SubsonicResponseBody body = SubsonicWriter.Body();
            body.ArtistInfo2 = new SubsonicArtistInfo();
            SubsonicWriter.Write(req, processor, body);
        }
    }
}
