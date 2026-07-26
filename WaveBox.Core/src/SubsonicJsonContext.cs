using System.Text.Json.Serialization;
using WaveBox.Core.ApiResponse.Subsonic;

namespace WaveBox.Core {
    // Source-generated System.Text.Json context for the Subsonic API surface (NativeAOT-safe).
    // SubsonicResponse is the single serialization root: every Subsonic DTO must be reachable
    // from its property graph or its metadata is never generated and serialization fails at
    // runtime under AOT.
    [JsonSourceGenerationOptions(WriteIndented = false)]
    [JsonSerializable(typeof(SubsonicResponse))]
    public partial class SubsonicJsonContext : JsonSerializerContext {
    }
}
