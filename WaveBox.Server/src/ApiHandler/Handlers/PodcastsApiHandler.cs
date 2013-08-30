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

		/// <summary>
		/// Process handles all functions needed for the Podcast API
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			IList<Podcast> listToReturn = new List<Podcast>();

			if (uri.UriPart(2) == null)
			{
				if (uri.Parameters.ContainsKey("action"))
				{
					string action = null;
					uri.Parameters.TryGetValue("action", out action);

					if (action == null)
					{
						processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Parameter 'action' contained an invalid value", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
					}

					else
					{
						if (action == "add")
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
									processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Parameter 'url' contained an invalid value", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
								}

								else if (keepCapTemp == null)
								{
									processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Parameter 'keepCap' contained an invalid value", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
								}
								else
								{
									if (!Int32.TryParse(keepCapTemp, out keepCap))
									{
										processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Parameter 'keepCap' contained an invalid value", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									}
									url = System.Web.HttpUtility.UrlDecode(url);
									Podcast pod = new Podcast.Factory().CreatePodcast(url, keepCap);

									pod.DownloadNewEpisodes();
									processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse(null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
							}
							else
							{
								processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Missing parameter for action 'add'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
							}
						}
						else if (action == "delete")
						{
							if (!(uri.Parameters.ContainsKey("id") || uri.Parameters.ContainsKey("episodeId")))
							{
								processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Missing parameter for action 'delete'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
								return;
							}
							else if (uri.Parameters.ContainsKey("id") && uri.Parameters.ContainsKey("episodeId"))
							{
								processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Ambiguous parameters for action 'delete'.  'delete' accepts either a id or a episodeId, but not both.", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
								return;
							}
							else if (uri.Parameters.ContainsKey("id"))
							{
								int id = 0;
								string idString = null;

								uri.Parameters.TryGetValue("id", out idString);
								if (Int32.TryParse(idString, out id))
								{
									processor.WriteJson(JsonConvert.SerializeObject(new PodcastActionResponse(null, new Podcast.Factory().CreatePodcast(id).Delete()), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
								else
								{
									processor.WriteJson(JsonConvert.SerializeObject(new PodcastActionResponse("Parameter 'id' contained an invalid value", false), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
							}
							else
							{
								int id = 0;
								string idString = null;

								uri.Parameters.TryGetValue("episodeId", out idString);
								if (Int32.TryParse(idString, out id))
								{
									processor.WriteJson(JsonConvert.SerializeObject(new PodcastActionResponse(null, new PodcastEpisode.Factory().CreatePodcastEpisode(id).Delete()), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
								else
								{
									processor.WriteJson(JsonConvert.SerializeObject(new PodcastActionResponse("Parameter 'episodeId' contained an invalid value", false), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
							}
						}
					}
				}

				listToReturn = Podcast.ListOfStoredPodcasts();
				processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse(null, listToReturn), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
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

					processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse(null, thisPodcast, epList), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
					return;
				}

				else
				{
					processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Invalid Podcast ID", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
					return;
				}
			}
		}
	}
}
