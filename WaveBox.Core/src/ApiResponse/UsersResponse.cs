using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class UsersResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("users")]
		public List<User> Users { get; set; }

		public UsersResponse(string error, List<User> users)
		{
			Error = error;
			Users = users;
		}
	}
}

