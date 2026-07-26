using System;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text.Json.Serialization;

namespace WaveBox.Core.Model {
    // See IItem: polymorphic serialization without a discriminator, for Newtonsoft-compatible output
    [JsonPolymorphic]
    [JsonDerivedType(typeof(MediaItem))]
    [JsonDerivedType(typeof(Song))]
    [JsonDerivedType(typeof(Video))]
    public interface IMediaItem : IItem {
        [JsonPropertyName("folderId")]
        int? FolderId { get; set; }

        [JsonPropertyName("fileType"), IgnoreRead, IgnoreWrite]
        FileType FileType { get; set; }

        [JsonPropertyName("duration")]
        int? Duration { get; set; }

        [JsonPropertyName("bitrate")]
        int? Bitrate { get; set; }

        [JsonPropertyName("fileSize")]
        long? FileSize { get; set; }

        [JsonPropertyName("lastModified")]
        long? LastModified { get; set; }

        [JsonPropertyName("fileName")]
        string FileName { get; set; }

        [JsonPropertyName("genreId")]
        int? GenreId { get; set; }

        [JsonPropertyName("genreName")]
        string GenreName { get; set; }

        void AddToPlaylist(Playlist thePlaylist, int index);

        void InsertMediaItem();
    }
}

