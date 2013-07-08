using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.Service.Services.Http;

namespace WaveBox.ApiHandler
{
	class StatsApiHandler : IApiHandler
	{
		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for StatsApiHandler
		/// </summary>
		public StatsApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process records play stats for artists, albums, songs
		/// </summary>
		public void Process()
		{
			// Ensure an event is present
			if (!Uri.Parameters.ContainsKey("event"))
			{
				Processor.WriteJson(JsonConvert.SerializeObject(new StatsResponse("Please specify an event parameter with comma separated list of events")));
				return;
			}

			// Split events into id, stat type, UNIX timestamp triples
			string[] events = Uri.Parameters["event"].Split(',');

			// Ensure data sent
			if (events.Length < 1)
			{
				// Report empty data list
				Processor.WriteJson(JsonConvert.SerializeObject(new StatsResponse("Event data list is empty")));
				return;
			}

			// Ensure events are in triples
			if (events.Length % 3 != 0)
			{
				Processor.WriteJson(JsonConvert.SerializeObject(new StatsResponse("Event data list must be in comma-separated triples of form itemId,statType,timestamp")));
				return;
			}

			// Iterate all events
			for (int i = 0; i < events.Length - 3; i += 3)
			{
				// Store item ID, stat type, and UNIX timestamp
				string itemId = events[i];
				string statType = events[i+1];
				string timeStamp = events[i+2];

				// Initialize to null defaults
				int itemIdInt = -1;
				StatType statTypeEnum = StatType.Unknown;
				long timeStampLong = -1;

				// Perform three checks for valid item ID, stat type, UNIX timestamp
				bool success = Int32.TryParse(itemId, out itemIdInt);
				if (success)
				{
					success = Enum.TryParse<StatType>(statType, true, out statTypeEnum);
				}
				if (success)
				{
					success = Int64.TryParse(timeStamp, out timeStampLong);
				}
				if (success)
				{
					// If all three are successful, generate an item type from the ID
					ItemType itemType = Item.ItemTypeForItemId(itemIdInt);

					// Case: type is song, stat is playcount
					if ((itemType == ItemType.Song) && (statTypeEnum == StatType.PLAYED))
					{
						// Also record a play for the artist, album, and folder
						Song song = new Song.Factory().CreateSong(itemIdInt);
						if ((object)song.AlbumId != null)
						{
							Stat.RecordStat((int)song.AlbumId, statTypeEnum, timeStampLong);
						}
						if ((object)song.ArtistId != null)
						{
							Stat.RecordStat((int)song.ArtistId, statTypeEnum, timeStampLong);
						}
						if ((object)song.FolderId != null)
						{
							Stat.RecordStat((int)song.FolderId, statTypeEnum, timeStampLong);
						}
					}

					// Record stats for the generic item
					Stat.RecordStat(itemIdInt, statTypeEnum, timeStampLong);
				}
			}

			// After all stat iterations, return a successful response
			Processor.WriteJson(JsonConvert.SerializeObject(new StatsResponse(null)));
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
	}
}
