using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace WaveBox.E2E.Tests {
    [Collection("E2E")]
    public class SubsonicStreamTests {
        private readonly WaveBoxServerFixture server;

        public SubsonicStreamTests(WaveBoxServerFixture server) {
            this.server = server;
        }

        [Fact]
        public async Task RawStreamRangeRequestReturns206() {
            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Get, "rest/stream?u=test&p=test&id=" + server.SongId + "&format=raw");
            request.Headers.Range = new RangeHeaderValue(100, 199);
            using (HttpResponseMessage response = await server.Client.SendAsync(request, TestContext.Current.CancellationToken)) {
                Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            }
        }

        [Fact]
        public async Task TranscodedStreamReturnsAudio() {
            Assert.SkipUnless(WaveBoxServerFixture.FfmpegPresent, "ffmpeg is not installed");

            byte[] audio = await server.Client.GetByteArrayAsync(
                "rest/stream?u=test&p=test&id=" + server.SongId + "&maxBitRate=32&format=mp3", TestContext.Current.CancellationToken);
            Assert.True(audio.Length > 0, "expected transcoded audio bytes, got none");
        }
    }
}
