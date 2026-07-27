using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace WaveBox.E2E.Tests {
    [Collection("E2E")]
    public class SubsonicSystemTests {
        private readonly WaveBoxServerFixture server;

        public SubsonicSystemTests(WaveBoxServerFixture server) {
            this.server = server;
        }

        [Fact]
        public async Task PingDefaultsToXmlEnvelope() {
            string body = await server.Client.GetStringAsync("rest/ping.view?u=test&p=test", TestContext.Current.CancellationToken);
            XDocument doc = XDocument.Parse(body);
            XNamespace ns = "http://subsonic.org/restapi";
            Assert.Equal(ns + "subsonic-response", doc.Root.Name);
            Assert.Equal("ok", doc.Root.Attribute("status").Value);
        }

        [Fact]
        public async Task PingJsonEnvelopeReportsOpenSubsonic() {
            JsonNode response = await SubsonicClient.Rest(server.Client, "ping", SubsonicClient.Auth);
            Assert.Equal("ok", response["status"].GetValue<string>());
            Assert.True(response["openSubsonic"].GetValue<bool>());
        }

        [Fact]
        public async Task WrongPasswordReturnsError40() {
            JsonNode response = await SubsonicClient.Rest(server.Client, "ping", "u=test&p=wrong&f=json");
            Assert.Equal(40, response["error"]["code"].GetValue<int>());
        }

        [Fact]
        public async Task TokenAuthReturnsError42() {
            JsonNode response = await SubsonicClient.Rest(server.Client, "ping", "u=test&t=deadbeef&s=salt&f=json");
            Assert.Equal(42, response["error"]["code"].GetValue<int>());
        }
    }
}
