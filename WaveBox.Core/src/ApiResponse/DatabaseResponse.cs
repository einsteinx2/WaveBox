using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class DatabaseResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("queries")]
        public IList<QueryLog> Queries { get; set; }

        public DatabaseResponse(string error, IList<QueryLog> queries) {
            Error = error;
            Queries = queries;
        }
    }
}

