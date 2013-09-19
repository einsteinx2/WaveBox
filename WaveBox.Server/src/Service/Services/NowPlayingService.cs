using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Ninject;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Server.Extensions;
using WaveBox.Static;

namespace WaveBox.Service.Services
{
	class NowPlayingService : IService
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "nowplaying"; } set { } }

		public bool Required { get { return false; } set { } }

		public bool Running { get; set; }

		// List of NowPlaying objects
		public IList<NowPlaying> Playing { get { return this.playing; } set { } }
		private List<NowPlaying> playing;

		public NowPlayingService()
		{
		}

		public bool Start()
		{
			// Initialize list
			this.playing = new List<NowPlaying>();

			return true;
		}

		public bool Stop()
		{
			// Clear list
			this.playing = null;

			return true;
		}

		public bool Register(User user, IMediaItem m, long? timestamp = null)
		{
			// Begin building object
			NowPlaying nowPlaying = new NowPlaying();

			// Capture username, user's current client name from session
			string userName = user.UserName;
			nowPlaying.UserName = userName;
			string clientName = user.CurrentSession.ClientName;

			// If no client name, use default
			clientName = clientName ?? "wavebox";
			nowPlaying.ClientName = clientName;

			// Check if client sent a timestamp (if not, use current time)
			if (timestamp == null)
			{
				timestamp = DateTime.Now.ToUniversalUnixTimestamp();
			}

			// Capture play time to set up automatic unregister on playback end
			nowPlaying.StartTime = timestamp;
			nowPlaying.EndTime = timestamp + Convert.ToInt32(m.Duration);

			// Start a timer, set to elapse and unregister this song exactly when it should finish playback
			nowPlaying.Timer = new Timer(Convert.ToInt32(m.Duration) * 1000);
			nowPlaying.Timer.Elapsed += delegate { this.Unregister(userName, clientName); };
			nowPlaying.Timer.Start();

			// Capture media item's type
			Type mediaType = m.GetType();

			// Handling for Song items
			if (mediaType.IsAssignableFrom(typeof(Song)))
			{
				// Box IMediaItem to Song
				Song s = (Song)m;
				nowPlaying.MediaItem = s;

				// Unregister any items with matching user and client
				this.Unregister(userName, clientName);

				// Report now playing
				playing.Add(nowPlaying);
				logger.IfInfo(String.Format("{0}@{1} Now Playing: {2} - {3} - {4} [{5}]",
					userName,
					clientName,
					s.ArtistName,
					s.AlbumName,
					s.SongName,
					Convert.ToInt32(s.Duration).ToTimeString()
				));
			}
			// Handling for Video items
			else if (mediaType.IsAssignableFrom(typeof(Video)))
			{
				// Box IMediaItem to Video
				Video v = (Video)m;
				nowPlaying.MediaItem = v;

				// Unregister any items with matching user and client
				this.Unregister(userName, clientName);

				// Report now playing
				playing.Add(nowPlaying);
				logger.IfInfo(String.Format("{0}@{1} Now Watching: {2} [{3}]",
					userName,
					clientName,
					v.FileName,
					Convert.ToInt32(v.Duration).ToTimeString()
				));
			}
			else
			{
				// Report unsupported media types
				logger.IfInfo("Media type not supported, skipping now playing registration...");
			}

			return true;
		}

		public bool Unregister(string userName, string clientName)
		{
			// Check for existence
			if (!this.Playing.Any(x => x.UserName == userName && x.ClientName == clientName))
			{
				return false;
			}

			// Grab instance
			NowPlaying nowPlaying = this.playing.Single(x => x.UserName == userName && x.ClientName == clientName);

			// Disable timer
			nowPlaying.Timer.Stop();
			nowPlaying.Timer = null;

			// Remove from list
			this.playing.Remove(nowPlaying);

			return true;
		}
	}
}
