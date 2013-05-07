using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using WaveBox.TcpServer.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class VideosApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for VideosApiHandler
		/// </summary>
		public VideosApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process returns a list of videos from WaveBox
		/// </summary>
		public void Process()
		{
			// Return list of videos
			List<Video> listOfVideos = new List<Video>();

			// Try to fetch video ID
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			// On successful ID, grab one video
			if (success)
			{
				listOfVideos.Add(new Video(id));
			}
			else
			{
				// Else, grab all videos
				listOfVideos = Video.AllVideos();
			}

			// Return video list in a response
			try
			{
				string json = JsonConvert.SerializeObject(new VideosResponse(null, listOfVideos), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
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
