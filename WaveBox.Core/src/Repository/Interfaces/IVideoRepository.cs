using System;
using System.Collections.Generic;

namespace WaveBox.Model.Repository
{
	public interface IVideoRepository
	{
		Video VideoForId(int videoId);
		List<Video> AllVideos();
		int CountVideos();
		long TotalVideoSize();
		long TotalVideoDuration();
		List<Video> SearchVideos(string field, string query, bool exact = true);
		List<Video> RangeVideos(char start, char end);
		List<Video> LimitVideos(int index, int duration = Int32.MinValue);
	}
}

