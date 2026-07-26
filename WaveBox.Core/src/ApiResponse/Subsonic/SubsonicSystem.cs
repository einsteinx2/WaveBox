using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WaveBox.Core.ApiResponse.Subsonic {
    public class SubsonicLicense {
        [JsonPropertyName("valid")]
        public bool Valid { get; set; } = true;

        [JsonPropertyName("email"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Email { get; set; }

        [JsonPropertyName("licenseExpires"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LicenseExpires { get; set; }
    }

    public class SubsonicExtension {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("versions")]
        public IList<int> Versions { get; set; }
    }

    public class SubsonicTokenInfo {
        [JsonPropertyName("username")]
        public string Username { get; set; }
    }

    public class SubsonicScanStatus {
        [JsonPropertyName("scanning")]
        public bool Scanning { get; set; }

        [JsonPropertyName("count"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Count { get; set; }
    }

    // Intentionally empty: WaveBox has no last.fm-style artist metadata, and every field of the
    // Subsonic artistInfo schema is optional.  Clients calling getArtistInfo(2) get a valid,
    // empty object rather than an error.
    public class SubsonicArtistInfo {
    }
}
