using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.Service.Services.Http;

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
			IList<Video> videos = new List<Video>();

			// Fetch video ID from parameters
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				if (!Int32.TryParse(Uri.Parameters["id"], out id))
				{
					string json = JsonConvert.SerializeObject(new VideosResponse("Parameter 'id' requires a valid integer", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Add video by ID to the list
				videos.Add(new Video.Factory().CreateVideo(id));
			}
			// Check for a request for range of videos
			else if (Uri.Parameters.ContainsKey("range"))
			{
				string[] range = Uri.Parameters["range"].Split(',');

				// Ensure valid range was parsed
				if (range.Length != 2)
				{
					string json = JsonConvert.SerializeObject(new VideosResponse("Parameter 'range' requires a valid, comma-separated character tuple", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Validate as characters
				char start, end;
				if (!Char.TryParse(range[0], out start) || !Char.TryParse(range[1], out end))
				{
					string json = JsonConvert.SerializeObject(new VideosResponse("Parameter 'range' requires characters which are single alphanumeric values", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Grab range of videos
				videos = Video.RangeVideos(start, end);
			}

			// Check for a request to limit/paginate videos, like SQL
			// Note: can be combined with range or all videos
			if (Uri.Parameters.ContainsKey("limit") && !Uri.Parameters.ContainsKey("id"))
			{
				string[] limit = Uri.Parameters["limit"].Split(',');

				// Ensure valid limit was parsed
				if (limit.Length < 1 || limit.Length > 2 )
				{
					string json = JsonConvert.SerializeObject(new VideosResponse("Parameter 'limit' requires a single integer, or a valid, comma-separated integer tuple", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Validate as integers
				int index = 0;
				int duration = Int32.MinValue;
				if (!Int32.TryParse(limit[0], out index))
				{
					string json = JsonConvert.SerializeObject(new VideosResponse("Parameter 'limit' requires a valid integer start index", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Ensure positive index
				if (index < 0)
				{
					string json = JsonConvert.SerializeObject(new VideosResponse("Parameter 'limit' requires a non-negative integer start index", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					Processor.WriteJson(json);
					return;
				}

				// Check for duration
				if (limit.Length == 2)
				{
					if (!Int32.TryParse(limit[1], out duration))
					{
						string json = JsonConvert.SerializeObject(new VideosResponse("Parameter 'limit' requires a valid integer duration", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						Processor.WriteJson(json);
						return;
					}

					// Ensure positive duration
					if (duration < 0)
					{
						string json = JsonConvert.SerializeObject(new VideosResponse("Parameter 'limit' requires a non-negative integer duration", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						Processor.WriteJson(json);
						return;
					}
				}

				// Check if results list already populated by range
				if (videos.Count > 0)
				{
					// No duration?  Return just specified number of videos
					if (duration == Int32.MinValue)
					{
						videos = videos.Skip(0).Take(index).ToList();
					}
					else
					{
						// Else, return videos starting at index, up to count duration
						videos = videos.Skip(index).Take(duration).ToList();
					}
				}
				else
				{
					// If no videos in list, grab directly using model method
					videos = Video.LimitVideos(index, duration);
				}
			}

			// Finally, if no videos already in list, send the whole list
			if (videos.Count == 0)
			{
				videos = Video.AllVideos();
			}

			try
			{
				// Send it!
				string json = JsonConvert.SerializeObject(new VideosResponse(null, videos), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
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
			public IList<Video> Videos { get; set; }

			public VideosResponse(string error, IList<Video> videos)
			{
				Error = error;
				Videos = videos;
			}
		}
	}
}
