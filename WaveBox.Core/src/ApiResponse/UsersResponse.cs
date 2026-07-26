using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class UsersResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("users")]
        public IList<User> Users { get; set; }

        public UsersResponse(string error, IList<User> users) {
            Error = error;
            Users = users;
        }
    }
}

