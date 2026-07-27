using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace WaveBox.E2E.Tests {
    [Collection("E2E")]
    public class SubsonicAnnotationTests {
        private readonly WaveBoxServerFixture server;

        public SubsonicAnnotationTests(WaveBoxServerFixture server) {
            this.server = server;
        }

        [Fact]
        public async Task StarGetStarredUnstarRoundTrip() {
            await SubsonicClient.Rest(server.Client, "star", SubsonicClient.Auth + "&id=" + server.SongId);
            try {
                JsonNode starred = await SubsonicClient.Rest(server.Client, "getStarred2", SubsonicClient.Auth);
                JsonArray songs = (JsonArray)starred["starred2"]["song"];
                Assert.Single(songs);
                Assert.NotNull(songs[0]["starred"]);
            } finally {
                await SubsonicClient.Rest(server.Client, "unstar", SubsonicClient.Auth + "&id=" + server.SongId);
            }

            JsonNode after = await SubsonicClient.Rest(server.Client, "getStarred2", SubsonicClient.Auth);
            JsonNode remaining = after["starred2"]["song"];
            Assert.True(remaining == null || ((JsonArray)remaining).Count == 0);
        }

        [Fact]
        public async Task ScrobbleRegistersNowPlayingAndRecentAlbums() {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await SubsonicClient.Rest(server.Client, "scrobble", SubsonicClient.Auth + "&id=" + server.SongId + "&time=" + nowMs);

            JsonNode nowPlaying = await SubsonicClient.Rest(server.Client, "getNowPlaying", SubsonicClient.Auth);
            Assert.Equal("test", nowPlaying["nowPlaying"]["entry"][0]["username"].GetValue<string>());

            JsonNode recent = await SubsonicClient.Rest(server.Client, "getAlbumList2", SubsonicClient.Auth + "&type=recent");
            Assert.True(((JsonArray)recent["albumList2"]["album"]).Count > 0);
        }
    }
}
