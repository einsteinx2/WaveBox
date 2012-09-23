using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using WaveBox.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class VideosApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		
		public VideosApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}
		
		public void Process()
		{
			List<Video> listOfVideos = new List<Video>();
			
			// Try to get the song id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}
			
			if (success)
			{
				listOfVideos.Add(new Video(id));
			}
			else
			{
				listOfVideos = Video.allVideos();
			}
			
			try
			{
				string json = JsonConvert.SerializeObject(new VideosResponse(null, listOfVideos), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch(Exception e)
			{
				Console.WriteLine("[SONGAPI] ERROR: " + e.ToString());
			}
		}
		
		private class VideosResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }
			
			[JsonProperty("videos")]
			public List<Video> Videos { get; set; }
			
			public VideosResponse(string error, List<Video> videos)
			{
				Error = error;
				Videos = videos;
			}
		}
	}
}
