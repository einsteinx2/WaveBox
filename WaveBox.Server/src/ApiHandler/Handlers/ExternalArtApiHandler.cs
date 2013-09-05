using System;
using WaveBox.Service.Services.Http;
using WaveBox.Core.Model;
using WaveBox.Core;
using Ninject;
using WaveBox.Core.Model.Repository;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using WaveBox.Core.ApiResponse;

namespace WaveBox.ApiHandler.Handlers
{
	class ExternalArtApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "externalart"; } }

		private readonly string fanArtApiKey = "";

		/// <summary>
		/// Process returns a file stream containing album art
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Check for the itemId
			if (!uri.Parameters.ContainsKey("id"))
			{
				processor.WriteErrorHeader();
				return;
			}

			// Convert to integer
			int itemId = 0;
			Int32.TryParse(uri.Parameters["id"], out itemId);

			// If art ID was invalid, write error header
			if (itemId == 0)
			{
				processor.WriteErrorHeader();
				return;
			}

			// Check for blur (value between 0 and 100)
			bool thumbnail = false;
			if (uri.Parameters.ContainsKey("thumbnail"))
			{
				Boolean.TryParse(uri.Parameters["thumbnail"], out thumbnail);
			}

			IItemRepository itemRepository = Injection.Kernel.Get<IItemRepository>();
			ItemType type = itemRepository.ItemTypeForItemId(itemId);

			// Only support artist art right now
			if (type == ItemType.Artist || type == ItemType.AlbumArtist)
			{
				string artistName = null;
				if (type == ItemType.Artist)
				{
					IArtistRepository artistRepository = Injection.Kernel.Get<IArtistRepository>();
					Artist artist = artistRepository.ArtistForId(itemId);
					if (artist != null)
					{
						artistName = artist.ArtistName;
					}
				}
				else
				{
					IAlbumArtistRepository albumArtistRepository = Injection.Kernel.Get<IAlbumArtistRepository>();
					AlbumArtist albumArtist = albumArtistRepository.AlbumArtistForId(itemId);
					if (albumArtist != null)
					{
						artistName = albumArtist.AlbumArtistName;
					}
				}

				if (artistName != null)
				{
					// Grab the artist info from MusicBrainz
					MusicBrainz.Artist result = MusicBrainz.Artist.Query(artistName);

					if (result != null && result.Id != null)
					{
						WebClient client = new WebClient();
						try
						{
							string url = "http://api.fanart.tv/webservice/artist/" + fanArtApiKey + "/" + result.Id + "/json/artistbackground/1/1/";
							string response = client.DownloadString(url);

							JObject o = JObject.Parse(response);
							JObject artistO = (JObject)o[result.GetName()];
							JArray artistBackground = (JArray)artistO["artistbackground"];
							if (artistBackground != null && artistBackground.Count > 0)
							{
								JObject firstBackground = (JObject)artistBackground[0];
								string artUrl = (string)firstBackground["url"];
								if (artUrl != null)
								{
									if (thumbnail)
										artUrl += "/preview";

									string json = JsonConvert.SerializeObject(new ExternalArtResponse(null, artUrl), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
									processor.WriteJson(json);
									return;
								}
							}
						}
						catch (Exception e)
						{
							logger.Error("Exception retreiving art info from FanArt: " + e);
						}
					}
				}
			}

			// If all else fails, return error
			processor.WriteErrorHeader();
		}
	}
}

