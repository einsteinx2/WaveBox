using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Model;
using WaveBox.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.ApiResponse;
using WaveBox.Core;

namespace WaveBox.ApiHandler.Handlers
{
	class VideosApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "videos"; } }

		/// <summary>
		/// Process returns a list of videos from WaveBox
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Return list of videos
			IList<Video> videos = new List<Video>();

			// Fetch video ID from parameters
			int id = 0;
			if (uri.Parameters.ContainsKey("id"))
			{
				if (!Int32.TryParse(uri.Parameters["id"], out id))
				{
					processor.WriteJson(new VideosResponse("Parameter 'id' requires a valid integer", null));
					return;
				}

				// Add video by ID to the list
				videos.Add(Injection.Kernel.Get<IVideoRepository>().VideoForId(id));
			}
			// Check for a request for range of videos
			else if (uri.Parameters.ContainsKey("range"))
			{
				string[] range = uri.Parameters["range"].Split(',');

				// Ensure valid range was parsed
				if (range.Length != 2)
				{
					processor.WriteJson(new VideosResponse("Parameter 'range' requires a valid, comma-separated character tuple", null));
					return;
				}

				// Validate as characters
				char start, end;
				if (!Char.TryParse(range[0], out start) || !Char.TryParse(range[1], out end))
				{
					processor.WriteJson(new VideosResponse("Parameter 'range' requires characters which are single alphanumeric values", null));
					return;
				}

				// Grab range of videos
				videos = Injection.Kernel.Get<IVideoRepository>().RangeVideos(start, end);
			}

			// Check for a request to limit/paginate videos, like SQL
			// Note: can be combined with range or all videos
			if (uri.Parameters.ContainsKey("limit") && !uri.Parameters.ContainsKey("id"))
			{
				string[] limit = uri.Parameters["limit"].Split(',');

				// Ensure valid limit was parsed
				if (limit.Length < 1 || limit.Length > 2 )
				{
					processor.WriteJson(new VideosResponse("Parameter 'limit' requires a single integer, or a valid, comma-separated integer tuple", null));
					return;
				}

				// Validate as integers
				int index = 0;
				int duration = Int32.MinValue;
				if (!Int32.TryParse(limit[0], out index))
				{
					processor.WriteJson(new VideosResponse("Parameter 'limit' requires a valid integer start index", null));
					return;
				}

				// Ensure positive index
				if (index < 0)
				{
					processor.WriteJson(new VideosResponse("Parameter 'limit' requires a non-negative integer start index", null));
					return;
				}

				// Check for duration
				if (limit.Length == 2)
				{
					if (!Int32.TryParse(limit[1], out duration))
					{
						processor.WriteJson(new VideosResponse("Parameter 'limit' requires a valid integer duration", null));
						return;
					}

					// Ensure positive duration
					if (duration < 0)
					{
						processor.WriteJson(new VideosResponse("Parameter 'limit' requires a non-negative integer duration", null));
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
					videos = Injection.Kernel.Get<IVideoRepository>().LimitVideos(index, duration);
				}
			}

			// Finally, if no videos already in list, send the whole list
			if (videos.Count == 0)
			{
				videos = Injection.Kernel.Get<IVideoRepository>().AllVideos();
			}

			// Send it!
			processor.WriteJson(new VideosResponse(null, videos));
		}
	}
}
