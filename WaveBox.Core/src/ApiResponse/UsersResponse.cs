using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse {
    public class UsersResponse : IApiResponse {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("users")]
        public IList<User> Users { get; set; }

        public UsersResponse(string error, IList<User> users) {
            Error = error;
            Users = users;
        }
    }
}

