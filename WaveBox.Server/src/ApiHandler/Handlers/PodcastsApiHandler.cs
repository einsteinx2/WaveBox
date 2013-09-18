using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.ApiHandler;
using WaveBox.Core.Model;
using WaveBox.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Core.ApiResponse;
using WaveBox.Core;

namespace WaveBox.ApiHandler.Handlers
{
	class PodcastsApiHandler : IApiHandler
	{
		public string Name { get { return "podcasts"; } }

		// Standard permissions
		public bool CheckPermission(User user, string action)
		{
			switch (action)
			{
				// Write
				case "add":
				case "delete":
					return user.HasPermission(Role.User);
				// Read
				case "read":
				default:
					return user.HasPermission(Role.Test);
			}

			return false;
		}

		/// <summary>
		/// Process handles all functions needed for the Podcast API
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			IList<Podcast> listToReturn = new List<Podcast>();

			if (uri.UriPart(2) == null)
			{
				if (uri.Action != null)
				{
					if (uri.Action == "add")
					{
						if ((uri.Parameters.ContainsKey("url")) && (uri.Parameters.ContainsKey("keepCap")))
						{
							string url = null;
							string keepCapTemp = null;
							int keepCap = 0;
							uri.Parameters.TryGetValue("url", out url);
							uri.Parameters.TryGetValue("keepCap", out keepCapTemp);

							if (url == null)
							{
								processor.WriteJson(new PodcastContentResponse("Parameter 'url' contained an invalid value", null));
							}

							else if (keepCapTemp == null)
							{
								processor.WriteJson(new PodcastContentResponse("Parameter 'keepCap' contained an invalid value", null));
							}
							else
							{
								if (!Int32.TryParse(keepCapTemp, out keepCap))
								{
									processor.WriteJson(new PodcastContentResponse("Parameter 'keepCap' contained an invalid value", null));
								}
								url = System.Web.HttpUtility.UrlDecode(url);
								Podcast pod = new Podcast.Factory().CreatePodcast(url, keepCap);

								pod.DownloadNewEpisodes();
								processor.WriteJson(new PodcastContentResponse(null, null));
								return;
							}
						}
						else
						{
							processor.WriteJson(new PodcastContentResponse("Missing parameter for action 'add'", null));
						}
					}
					else if (uri.Action == "delete")
					{
						if (!(uri.Id != null || uri.Parameters.ContainsKey("episodeId")))
						{
							processor.WriteJson(new PodcastContentResponse("Missing parameter for action 'delete'", null));
							return;
						}
						else if (uri.Id != null && uri.Parameters.ContainsKey("episodeId"))
						{
							processor.WriteJson(new PodcastContentResponse("Ambiguous parameters for action 'delete'.  'delete' accepts either a id or a episodeId, but not both.", null));
							return;
						}
						else if (uri.Id != null)
						{
							processor.WriteJson(new PodcastActionResponse(null, new Podcast.Factory().CreatePodcast((int)uri.Id).Delete()));
							return;
						}
						else
						{
							int id = 0;
							string idString = null;

							uri.Parameters.TryGetValue("episodeId", out idString);
							if (Int32.TryParse(idString, out id))
							{
								processor.WriteJson(new PodcastActionResponse(null, new PodcastEpisode.Factory().CreatePodcastEpisode(id).Delete()));
								return;
							}
							else
							{
								processor.WriteJson(new PodcastActionResponse("Parameter 'episodeId' contained an invalid value", false));
								return;
							}
						}
					}
				}

				listToReturn = Podcast.ListOfStoredPodcasts();
				processor.WriteJson(new PodcastContentResponse(null, listToReturn));
				return;
			}
			else
			{
				int id = 0;
				Int32.TryParse(uri.UriPart(2), out id);

				if (id != 0)
				{
					Podcast thisPodcast = new Podcast.Factory().CreatePodcast(id);
					IList<PodcastEpisode> epList = thisPodcast.ListOfStoredEpisodes();

					processor.WriteJson(new PodcastContentResponse(null, thisPodcast, epList));
					return;
				}
				else
				{
					processor.WriteJson(new PodcastContentResponse("Invalid Podcast ID", null));
					return;
				}
			}
		}
	}
}
