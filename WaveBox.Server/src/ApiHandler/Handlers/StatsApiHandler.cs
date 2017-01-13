using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Service;
using WaveBox.Service.Services;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler {
    class StatsApiHandler : IApiHandler {
        public string Name { get { return "stats"; } }

        // Only standard user and up may report stats
        public bool CheckPermission(User user, string action) {
            return user.HasPermission(Role.User);
        }

        /// <summary>
        /// Process records play stats for artists, albums, songs
        /// </summary>
        public void Process(UriWrapper uri, IHttpProcessor processor, User user) {
            // Ensure an event is present
            if (!uri.Parameters.ContainsKey("event")) {
                processor.WriteJson(new StatsResponse("Please specify an event parameter with comma separated list of events"));
                return;
            }

            // Split events into id, stat type, UNIX timestamp triples
            string[] events = uri.Parameters["event"].Split(',');

            // Ensure data sent
            if (events.Length < 1) {
                // Report empty data list
                processor.WriteJson(new StatsResponse("Event data list is empty"));
                return;
            }

            // Ensure events are in triples
            if (events.Length % 3 != 0) {
                processor.WriteJson(new StatsResponse("Event data list must be in comma-separated triples of form itemId,statType,timestamp"));
                return;
            }

            // Iterate all events
            for (int i = 0; i <= events.Length - 3; i += 3) {
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
                if (success) {
                    success = Enum.TryParse<StatType>(statType, true, out statTypeEnum);
                }
                if (success) {
                    success = Int64.TryParse(timeStamp, out timeStampLong);
                }
                if (success) {
                    // If all three are successful, generate an item type from the ID
                    ItemType itemType = Injection.Kernel.Get<IItemRepository>().ItemTypeForItemId(itemIdInt);

                    // Case: type is song, stat is playcount
                    if ((itemType == ItemType.Song) && (statTypeEnum == StatType.PLAYED)) {
                        // Also record a play for the artist, album, and folder
                        Song song = Injection.Kernel.Get<ISongRepository>().SongForId(itemIdInt);

                        // Trigger now playing service if available
                        NowPlayingService nowPlaying = (NowPlayingService)ServiceManager.GetInstance("nowplaying");
                        if (nowPlaying != null) {
                            nowPlaying.Register(user, song, timeStampLong);
                        }

                        if ((object)song.AlbumId != null) {
                            Injection.Kernel.Get<IStatRepository>().RecordStat((int)song.AlbumId, statTypeEnum, timeStampLong);
                        }
                        if ((object)song.ArtistId != null) {
                            Injection.Kernel.Get<IStatRepository>().RecordStat((int)song.ArtistId, statTypeEnum, timeStampLong);
                        }
                        if ((object)song.FolderId != null) {
                            Injection.Kernel.Get<IStatRepository>().RecordStat((int)song.FolderId, statTypeEnum, timeStampLong);
                        }
                    }

                    // Record stats for the generic item
                    Injection.Kernel.Get<IStatRepository>().RecordStat(itemIdInt, statTypeEnum, timeStampLong);
                }
            }

            // After all stat iterations, return a successful response
            processor.WriteJson(new StatsResponse(null));
        }
    }
}
