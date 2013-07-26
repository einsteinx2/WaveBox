using System;
using Newtonsoft.Json;
using WaveBox.Core.Model;
using System.Collections.Generic;

namespace WaveBox.Core.ApiResponse
{
	public class FoldersResponse
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("folders")]
		public List<Folder> Folders { get; set; }

		[JsonProperty("containingFolder")]
		public Folder ContainingFolder { get; set; }

		[JsonProperty("songs")]
		public List<Song> Songs { get; set; }

		[JsonProperty("videos")]
		public List<Video> Videos { get; set; }

		public FoldersResponse(string error, Folder containingFolder, List<Folder> folders, List<Song> songs, List<Video>videos)
		{
			Error = error;
			ContainingFolder = containingFolder;
			Folders = folders;
			Songs = songs;
			Videos = videos;
		}
	}
}

