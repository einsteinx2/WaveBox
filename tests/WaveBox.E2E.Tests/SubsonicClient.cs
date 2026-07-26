using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace WaveBox.E2E.Tests {
    internal static class SubsonicClient {
        /// <summary>Standard user credentials + JSON format, matching the users seeded at server startup.</summary>
        public const string Auth = "u=test&p=test&f=json";

        public static async Task<JsonNode> GetJson(HttpClient client, string pathAndQuery) {
            string body = await client.GetStringAsync(pathAndQuery);
            return JsonNode.Parse(body);
        }

        /// <summary>GETs a /rest endpoint and unwraps the "subsonic-response" envelope.</summary>
        public static async Task<JsonNode> Rest(HttpClient client, string endpoint, string query) {
            JsonNode root = await GetJson(client, "rest/" + endpoint + "?" + query);
            return root["subsonic-response"];
        }
    }
}
