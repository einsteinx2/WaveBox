using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace WaveBox.E2E.Tests {
    [Collection("E2E")]
    public class SubsonicPlaylistTests {
        private readonly WaveBoxServerFixture server;

        public SubsonicPlaylistTests(WaveBoxServerFixture server) {
            this.server = server;
        }

        [Fact]
        public async Task PlaylistLifecycleWithDuplicateSongIds() {
            // Duplicate songId parameters must both apply
            JsonNode created = await SubsonicClient.Rest(server.Client, "createPlaylist",
                SubsonicClient.Auth + "&name=E2EList&songId=" + server.SongId + "&songId=" + server.SongId);
            Assert.Equal(2, created["playlist"]["songCount"].GetValue<int>());
            Assert.Equal(2, ((JsonArray)created["playlist"]["entry"]).Count);

            JsonNode playlists = await SubsonicClient.Rest(server.Client, "getPlaylists", SubsonicClient.Auth);
            JsonNode match = ((JsonArray)playlists["playlists"]["playlist"])
                .First(p => p["name"].GetValue<string>() == "E2EList");
            string playlistId = match["id"].GetValue<string>();

            // Remove index 0 and add the song back: count stays at 2
            await SubsonicClient.Rest(server.Client, "updatePlaylist",
                SubsonicClient.Auth + "&playlistId=" + playlistId + "&songIndexToRemove=0&songIdToAdd=" + server.SongId);
            JsonNode updated = await SubsonicClient.Rest(server.Client, "getPlaylist", SubsonicClient.Auth + "&id=" + playlistId);
            Assert.Equal(2, updated["playlist"]["songCount"].GetValue<int>());
            Assert.Equal(2, ((JsonArray)updated["playlist"]["entry"]).Count);

            JsonNode deleted = await SubsonicClient.Rest(server.Client, "deletePlaylist", SubsonicClient.Auth + "&id=" + playlistId);
            Assert.Equal("ok", deleted["status"].GetValue<string>());
        }
    }
}
