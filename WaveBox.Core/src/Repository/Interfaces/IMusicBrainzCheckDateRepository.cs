using System;
using WaveBox.Core.Model;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IMusicBrainzCheckDateRepository
	{
		IList<MusicBrainzCheckDate> AllCheckDates();
		IList<MusicBrainzCheckDate> AllCheckDatesOlderThan(long timestamp);
		bool InsertMusicBrainzCheckDate(MusicBrainzCheckDate checkDate, bool replace = false);
	}
}
