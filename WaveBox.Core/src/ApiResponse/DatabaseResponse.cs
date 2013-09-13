using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.ApiResponse
{
	public class DatabaseResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("queries")]
		public IList<QueryLog> Queries { get; set; }

		public DatabaseResponse(string error, IList<QueryLog> queries)
		{
			Error = error;
			Queries = queries;
		}
	}
}

