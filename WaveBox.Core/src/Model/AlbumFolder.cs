using System;

namespace WaveBox.Core.Model {
    // Result row mapping an album to the folder that holds its songs (MIN(FolderId) when a
    // multi-disc album spans subfolders).  Lets folder-flavored Subsonic endpoints hand out
    // browsable folder ids for tag-derived album lists.  ORM-mapped: rooted in ModelTypeRegistry.
    public class AlbumFolder {
        public int? AlbumId { get; set; }

        public int? FolderId { get; set; }
    }
}
