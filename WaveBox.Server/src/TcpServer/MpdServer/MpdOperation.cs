using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WaveBox.Model;
using WaveBox.Static;

namespace WaveBox.TcpServer.Mpd
{
	public static class MpdOperation
	{
		// Special thanks to Mopidy, for huge help with the lacking MPD documentation
		// http://docs.mopidy.com/en/v0.14.1/_modules/mopidy/frontends/mpd/protocol/music_db/

		//private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// MpdCache, which will help us store lists which require a full table scan
		//private static MpdCache cache = new MpdCache();
	}
}
