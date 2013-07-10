using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.ApiHandler;
using WaveBox.Core.Injected;
using WaveBox.Model;
using WaveBox.PodcastManagement;
using WaveBox.Static;
using WaveBox.Service.Services.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class PodcastApiHandler : IApiHandler
	{
		public string Name { get { return "podcasts"; } set { } }

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for PodcastApiHandler
		/// </summary>
		public PodcastApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process handles all functions needed for the Podcast API
		/// </summary>
		public void Process()
		{
			List<Podcast> listToReturn = new List<Podcast>();

			if (Uri.UriPart(2) == null)
			{
				if (Uri.Parameters.ContainsKey("action"))
				{
					string action = null;
					Uri.Parameters.TryGetValue("action", out action);

					if (action == null)
					{
						Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Parameter 'action' contained an invalid value", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
					}

					else
					{
						if (action == "add")
						{
							if ((Uri.Parameters.ContainsKey("url")) && (Uri.Parameters.ContainsKey("keepCap")))
							{
								string url = null;
								string keepCapTemp = null;
								int keepCap = 0;
								Uri.Parameters.TryGetValue("url", out url);
								Uri.Parameters.TryGetValue("keepCap", out keepCapTemp);

								if (url == null)
								{
									Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Parameter 'url' contained an invalid value", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
								}

								else if (keepCapTemp == null)
								{
									Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Parameter 'keepCap' contained an invalid value", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
								}
								else 
								{
									if (!Int32.TryParse(keepCapTemp, out keepCap))
									{
										Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Parameter 'keepCap' contained an invalid value", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									}
									url = System.Web.HttpUtility.UrlDecode(url);
									Podcast pod = new Podcast.Factory().CreatePodcast(url, keepCap);

									pod.DownloadNewEpisodes();
									Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse(null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
							}
							else
							{
								Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Missing parameter for action 'add'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
							}
						}
						else if (action == "delete")
						{
							if (!(Uri.Parameters.ContainsKey("id") || Uri.Parameters.ContainsKey("episodeId")))
							{
								Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Missing parameter for action 'delete'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
								return;
							}
							else if (Uri.Parameters.ContainsKey("id") && Uri.Parameters.ContainsKey("episodeId"))
							{
								Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Ambiguous parameters for action 'delete'.  'delete' accepts either a id or a episodeId, but not both.", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
								return;
							}
							else if (Uri.Parameters.ContainsKey("id"))
							{
								int id = 0;
								string idString = null;

								Uri.Parameters.TryGetValue("id", out idString);
								if (Int32.TryParse(idString, out id))
								{
									Processor.WriteJson(JsonConvert.SerializeObject(new PodcastActionResponse(null, new Podcast.Factory().CreatePodcast(id).Delete()), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
								else
								{
									Processor.WriteJson(JsonConvert.SerializeObject(new PodcastActionResponse("Parameter 'id' contained an invalid value", false), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
							}
							else
							{
								int id = 0;
								string idString = null;

								Uri.Parameters.TryGetValue("episodeId", out idString);
								if (Int32.TryParse(idString, out id))
								{
									Processor.WriteJson(JsonConvert.SerializeObject(new PodcastActionResponse(null, new PodcastEpisode.Factory().CreatePodcastEpisode(id).Delete()), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
								else
								{
									Processor.WriteJson(JsonConvert.SerializeObject(new PodcastActionResponse("Parameter 'episodeId' contained an invalid value", false), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
									return;
								}
							}
						}
					}
				}

				listToReturn = Podcast.ListOfStoredPodcasts();
				Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse(null, listToReturn), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
				return;
			}
			else
			{
				int id = 0;
				Int32.TryParse(Uri.UriPart(2), out id);

				if (id != 0)
				{
					Podcast thisPodcast = new Podcast.Factory().CreatePodcast(id);
					List<PodcastEpisode> epList = thisPodcast.ListOfStoredEpisodes();

					Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse(null, thisPodcast, epList), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
					return;
				}

				else
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new PodcastContentResponse("Invalid Podcast ID", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting));
					return;
				}
			}
		}

		private class PodcastContentResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("podcasts")]
			public List<Podcast> Podcasts { get; set; }

			[JsonProperty("episodes")]
			public List<PodcastEpisode> Episodes { get; set; }

			public PodcastContentResponse(string error, List<Podcast> podcasts)
			{
				Error = error;
				Podcasts = podcasts;
				Episodes = null;
			}

			public PodcastContentResponse(string error, Podcast podcast, List<PodcastEpisode> episodes)
			{
				Error = error;
				Podcasts = new List<Podcast> { podcast };
				Episodes = episodes;
			}
		}

		private class PodcastActionResponse
		{
			[JsonProperty("error")]
			public bool Success { get; set; }

			[JsonProperty("success")]
			public string ErrorMessage { get; set; }

			public PodcastActionResponse(string errorMessage, bool success)
			{
				ErrorMessage = errorMessage;
				Success = success;
			}
		}
	}
}
