using System;
using System.Text.Json.Serialization;
using WaveBox.Core.Model;
using System.Collections.Generic;

namespace WaveBox.Core.ApiResponse {
    public class FoldersResponse : IApiResponse {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("folders")]
        public IList<Folder> Folders { get; set; }

        [JsonPropertyName("containingFolder")]
        public Folder ContainingFolder { get; set; }

        [JsonPropertyName("songs")]
        public IList<Song> Songs { get; set; }

        [JsonPropertyName("videos")]
        public IList<Video> Videos { get; set; }

        [JsonPropertyName("sectionPositions")]
        public PairList<string, int> SectionPositions { get; set; }

        public FoldersResponse(string error, Folder containingFolder, IList<Folder> folders, IList<Song> songs, IList<Video>videos, PairList<string, int> sectionPositions) {
            Error = error;
            ContainingFolder = containingFolder;
            Folders = folders;
            Songs = songs;
            Videos = videos;
            SectionPositions = sectionPositions;
        }
    }
}

