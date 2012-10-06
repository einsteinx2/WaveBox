using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Singletons;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using WaveBox.Http;

namespace WaveBox.ApiHandler
{
	public class StatsApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		
		public StatsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}
		
		public void Process()
		{
			if (Uri.Parameters.ContainsKey("event"))
			{
				string eventString = Uri.Parameters["event"];
				string[] events = null;

				events = eventString.Split(',');
				if(events.Length > 0)
				{
					if (events.Length % 3 == 0)
					{
						for (int i = 0; i < events.Length - 3; i += 3)
						{
							string itemId = events[i];
							string statType = events[i+1];
							string timeStamp = events[i+2];

							int itemIdInt = -1;
							StatType statTypeEnum = StatType.Unknown;
							long timeStampLong = -1;
							bool success = Int32.TryParse(itemId, out itemIdInt);
							if (success)
								success = Enum.TryParse<StatType>(statType, true, out statTypeEnum);
							if (success)
								success = Int64.TryParse(timeStamp, out timeStampLong);

							if (success)
							{
								ItemType itemType = Item.ItemTypeForItemId(itemIdInt);
								if (itemType == ItemType.Song && statTypeEnum == StatType.PLAYED)
								{
									// Also record a play for the artist, album, and folder
									Song song = new Song(itemIdInt);
									if ((object)song.AlbumId != null)
										Stat.RecordStat(song.AlbumId, statTypeEnum, timeStampLong);
									if ((object)song.ArtistId != null)
										Stat.RecordStat(song.ArtistId, statTypeEnum, timeStampLong);
									if ((object)song.FolderId != null)
										Stat.RecordStat(song.FolderId, statTypeEnum, timeStampLong);
								}

								Stat.RecordStat(itemIdInt, statTypeEnum, timeStampLong);
							}
						}

						Processor.WriteJson(JsonConvert.SerializeObject(new StatsResponse(null)));
					}
					else
					{
						Processor.WriteJson(JsonConvert.SerializeObject(new StatsResponse("Event list not a multiple of 3, did you forget a field somewhere?")));
					}
				}
				else 
				{
					Processor.WriteJson(JsonConvert.SerializeObject(new StatsResponse("Please specify an event or comma delimited list of events")));
				}
			}
			else
			{
				Processor.WriteJson(JsonConvert.SerializeObject(new StatsResponse("Please specify an event or comma delimited list of events")));
			}
		}
		
		private class StatsResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }
			
			public StatsResponse(string error)
			{
				Error = error;
			}
		}

		/*// Should find a better place to put these
		public static long ToUniversalUnixTimestamp(this DateTime dateTime)
		{
			return (long)(dateTime.ToUniversalTime() - new DateTime (1970, 1, 1).ToUniversalTime()).TotalSeconds;
		}
		
		public static long ToLocalUnixTimestamp(this DateTime dateTime)
		{
			return (long)(dateTime.ToLocalTime() - new DateTime (1970, 1, 1).ToLocalTime()).TotalSeconds;
		}*/
	}
}

