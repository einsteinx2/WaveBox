using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace WaveBox.E2E.Tests {
    [Collection("E2E")]
    public class ApiKeyTests {
        private readonly WaveBoxServerFixture server;

        public ApiKeyTests(WaveBoxServerFixture server) {
            this.server = server;
        }

        [Fact]
        public async Task ApiKeyLifecycle() {
            // Generate a key through the legacy admin API
            JsonNode login = await SubsonicClient.GetJson(server.Client, "api/login?u=admin&p=admin");
            string adminSession = login["sessionId"].GetValue<string>();

            JsonNode users = await SubsonicClient.GetJson(server.Client, "api/users?s=" + adminSession);
            JsonNode admin = ((JsonArray)users["users"]).First(u => u["userName"].GetValue<string>() == "admin");
            string adminId = admin["userId"].ToString();

            JsonNode generated = await SubsonicClient.GetJson(server.Client,
                "api/users/" + adminId + "?s=" + adminSession + "&action=generateApiKey");
            string apiKey = generated["users"][0]["apiKey"].GetValue<string>();
            Assert.False(string.IsNullOrEmpty(apiKey));

            // apiKey authenticates against /rest
            JsonNode ping = await SubsonicClient.Rest(server.Client, "ping", "apiKey=" + apiKey + "&f=json");
            Assert.Equal("ok", ping["status"].GetValue<string>());

            JsonNode tokenInfo = await SubsonicClient.Rest(server.Client, "tokenInfo", "apiKey=" + apiKey + "&f=json");
            Assert.Equal("admin", tokenInfo["tokenInfo"]["username"].GetValue<string>());

            // Conflicting auth mechanisms are rejected with code 43
            JsonNode conflict = await SubsonicClient.Rest(server.Client, "ping", "apiKey=" + apiKey + "&u=test&f=json");
            Assert.Equal(43, conflict["error"]["code"].GetValue<int>());
        }
    }
}
