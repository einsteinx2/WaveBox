using System;
using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core.Extensions;
using Ninject;

namespace WaveBox.Core.Model.Repository
{
	public class MusicBrainzCheckDateRepository : IMusicBrainzCheckDateRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public MusicBrainzCheckDateRepository(IDatabase database)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}

			this.database = database;
		}

		public IList<MusicBrainzCheckDate> AllCheckDates()
		{
			return this.database.GetList<MusicBrainzCheckDate>("SELECT * FROM MusicBrainzCheckDate");
		}

		public IList<MusicBrainzCheckDate> AllCheckDatesOlderThan(long timestamp)
		{
			return this.database.GetList<MusicBrainzCheckDate>("SELECT * FROM MusicBrainzCheckDate WHERE Timestamp < ?", timestamp);
		}

		public bool InsertMusicBrainzCheckDate(MusicBrainzCheckDate checkDate, bool replace = false)
		{
			return this.database.InsertObject<MusicBrainzCheckDate>(checkDate, replace ? InsertType.Replace : InsertType.InsertOrIgnore) > 0;
		}
	}
}
