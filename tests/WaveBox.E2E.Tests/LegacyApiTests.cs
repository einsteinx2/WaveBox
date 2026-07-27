using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace WaveBox.E2E.Tests {
    [Collection("E2E")]
    public class LegacyApiTests {
        private readonly WaveBoxServerFixture server;

        public LegacyApiTests(WaveBoxServerFixture server) {
            this.server = server;
        }

        [Fact]
        public void LoginReturnsSession() {
            Assert.False(string.IsNullOrEmpty(server.Session));
        }

        [Fact]
        public async Task StatusReportsVersionWithoutError() {
            JsonNode status = await SubsonicClient.GetJson(server.Client, "api/status?s=" + server.Session);
            // JSON null deserializes to a null JsonNode reference
            Assert.Null(status["error"]);
            Assert.NotNull(status["status"]["version"]);
        }

        [Fact]
        public async Task AlbumsEndpointRespondsWithoutError() {
            JsonNode albums = await SubsonicClient.GetJson(server.Client, "api/albums?s=" + server.Session);
            Assert.Null(albums["error"]);
        }

        [Fact]
        public async Task ScannedSongHasParsedTagMetadata() {
            JsonNode songs = await SubsonicClient.GetJson(server.Client, "api/songs?s=" + server.Session);
            JsonNode song = songs["songs"][0];
            Assert.Equal("Test Song", song["songName"].GetValue<string>());
            Assert.Equal("Test Artist", song["artistName"].GetValue<string>());
        }

        [Fact]
        public async Task StreamRangeRequestReturns206() {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/stream/" + server.SongId + "?s=" + server.Session);
            request.Headers.Range = new RangeHeaderValue(100, 199);
            using (HttpResponseMessage response = await server.Client.SendAsync(request, TestContext.Current.CancellationToken)) {
                Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            }
        }
    }
}
