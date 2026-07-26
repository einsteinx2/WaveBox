using System;

namespace WaveBox.Core.Model {
    // Result row for GROUP BY aggregate queries (e.g. song count and total duration per album,
    // album count per album artist or genre).  ORM-mapped: rooted in ModelTypeRegistry.
    public class GroupCount {
        public int? GroupId { get; set; }

        public int Count { get; set; }

        public long Total { get; set; }
    }
}
