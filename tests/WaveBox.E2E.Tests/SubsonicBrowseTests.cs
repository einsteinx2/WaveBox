using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace WaveBox.E2E.Tests {
    [Collection("E2E")]
    public class SubsonicBrowseTests {
        private readonly WaveBoxServerFixture server;

        public SubsonicBrowseTests(WaveBoxServerFixture server) {
            this.server = server;
        }

        [Fact]
        public async Task Id3BrowseChainFindsFixtureSong() {
            JsonNode artists = await SubsonicClient.Rest(server.Client, "getArtists", SubsonicClient.Auth);
            string artistId = artists["artists"]["index"][0]["artist"][0]["id"].GetValue<string>();
            Assert.False(string.IsNullOrEmpty(artistId));

            JsonNode artist = await SubsonicClient.Rest(server.Client, "getArtist", SubsonicClient.Auth + "&id=" + artistId);
            string albumId = artist["artist"]["album"][0]["id"].GetValue<string>();
            Assert.False(string.IsNullOrEmpty(albumId));

            JsonNode album = await SubsonicClient.Rest(server.Client, "getAlbum", SubsonicClient.Auth + "&id=" + albumId);
            JsonNode song = album["album"]["song"][0];
            Assert.Equal("Test Song", song["title"].GetValue<string>());
            Assert.True(song["duration"].GetValue<int>() > 0);
            // OpenSubsonic ids must be strings, not numbers
            Assert.Equal(System.Text.Json.JsonValueKind.String, song["id"].GetValueKind());
        }

        [Fact]
        public async Task Search3FindsSongAlbumAndArtist() {
            JsonNode result = await SubsonicClient.Rest(server.Client, "search3", SubsonicClient.Auth + "&query=Test");
            JsonNode searchResult = result["searchResult3"];
            Assert.True(((JsonArray)searchResult["song"]).Count > 0);
            Assert.True(((JsonArray)searchResult["album"]).Count > 0);
            Assert.True(((JsonArray)searchResult["artist"]).Count > 0);
        }

        [Fact]
        public async Task AlbumList2NewestIsNonEmpty() {
            JsonNode result = await SubsonicClient.Rest(server.Client, "getAlbumList2", SubsonicClient.Auth + "&type=newest");
            Assert.True(((JsonArray)result["albumList2"]["album"]).Count > 0);
        }

        [Fact]
        public async Task AlbumListEntriesBrowseAsFolders() {
            JsonNode list = await SubsonicClient.Rest(server.Client, "getAlbumList", SubsonicClient.Auth + "&type=newest");
            string directoryId = list["albumList"]["album"][0]["id"].GetValue<string>();

            JsonNode directory = await SubsonicClient.Rest(server.Client, "getMusicDirectory", SubsonicClient.Auth + "&id=" + directoryId);
            JsonArray children = (JsonArray)directory["directory"]["child"];
            Assert.Contains(children, c => c["title"].GetValue<string>() == "Test Song");
        }
    }
}
