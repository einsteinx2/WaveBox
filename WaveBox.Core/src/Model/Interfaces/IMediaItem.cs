using System;
using System.IO;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;

namespace WaveBox.Core.Model {
    public interface IMediaItem : IItem {
        [JsonProperty("folderId")]
        int? FolderId { get; set; }

        [JsonProperty("fileType"), IgnoreRead, IgnoreWrite]
        FileType FileType { get; set; }

        [JsonProperty("duration")]
        int? Duration { get; set; }

        [JsonProperty("bitrate")]
        int? Bitrate { get; set; }

        [JsonProperty("fileSize")]
        long? FileSize { get; set; }

        [JsonProperty("lastModified")]
        long? LastModified { get; set; }

        [JsonProperty("fileName")]
        string FileName { get; set; }

        [JsonProperty("genreId")]
        int? GenreId { get; set; }

        [JsonProperty("genreName")]
        string GenreName { get; set; }

        void AddToPlaylist(Playlist thePlaylist, int index);

        void InsertMediaItem();
    }
}

