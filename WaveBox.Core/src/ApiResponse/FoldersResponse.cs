using System;
using Newtonsoft.Json;
using WaveBox.Core.Model;
using System.Collections.Generic;

namespace WaveBox.Core.ApiResponse
{
	public class FoldersResponse : IApiResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("folders")]
		public IList<Folder> Folders { get; set; }

		[JsonProperty("containingFolder")]
		public Folder ContainingFolder { get; set; }

		[JsonProperty("songs")]
		public IList<Song> Songs { get; set; }

		[JsonProperty("videos")]
		public IList<Video> Videos { get; set; }

		[JsonProperty("sectionPositions")]
		public PairList<string, int> SectionPositions { get; set; }

		public FoldersResponse(string error, Folder containingFolder, IList<Folder> folders, IList<Song> songs, IList<Video>videos, PairList<string, int> sectionPositions)
		{
			Error = error;
			ContainingFolder = containingFolder;
			Folders = folders;
			Songs = songs;
			Videos = videos;
			SectionPositions = sectionPositions;
		}
	}
}

