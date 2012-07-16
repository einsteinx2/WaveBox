using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using Un4seen.Bass;

namespace WaveBox.DataModel.Singletons
{
	class Jukebox
	{
		private static Jukebox instance;
		public static Jukebox Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new Jukebox();
				}

				return instance;
			}
		}

		public bool IsInitialized { get; set; }

		public bool IsPlaying { get; set; }

		public int CurrentIndex { get; set; }

		private const string _PLAYLIST_NAME = "jukeboxQPbjnbh2JPU5NGxhXiiQ";
		private static Playlist playlist;

		private int currentStream;

		private Jukebox()
		{
			playlist = new Playlist(_PLAYLIST_NAME);
			if(playlist.PlaylistId == 0)
			{
				// playlist doesn't exist, so create it.
				playlist.createPlaylist();
			}
		}

		public double Progress()
		{
			if (IsInitialized && currentStream != 0)
			{
				long bytePosition = Bass.BASS_ChannelGetPosition(currentStream, BASSMode.BASS_POS_BYTES | BASSMode.BASS_POS_DECODE);
				double seconds = Bass.BASS_ChannelBytes2Seconds(currentStream, bytePosition);
				return seconds;
			}

			return 0.0;
		}

		public void Play()
		{
			if (currentStream != 0)
			{
				Bass.BASS_Start();
				IsPlaying = true;
			}
		}

		public void Pause()
		{
			if (IsPlaying && currentStream != 0)
			{
				Bass.BASS_Pause();
				IsPlaying = false;
			}
		}

		public void Stop()
		{
			if (IsInitialized)
			{
				BassFree();
			}
		}

		public void Prev()
		{
			CurrentIndex = CurrentIndex - 1 < 0 ? 0 : CurrentIndex - 1;
			PlaySongAtIndex(CurrentIndex);
		}

		public void Next ()
		{
			CurrentIndex = CurrentIndex + 1;

			if (CurrentIndex >= playlist.PlaylistCount) {
				CurrentIndex = CurrentIndex - 1;
				Stop ();
			} else {
				PlaySongAtIndex (CurrentIndex);
			}
		}

		public void PlaySongAtIndex(int index)
		{
			MediaItem item = playlist.MediaItemAtIndex(index);
			Console.WriteLine("[JUKEBOX] Playing song: " + item.FileName);

			if (item != null)
			{
				if (item.ItemTypeId == (int)ItemType.SONG)
				{
					// set the current index
					CurrentIndex = index;

					// re-initialize bass
					BassInit();

					// create the stream
					string path = item.File().Name;
					currentStream = Bass.BASS_StreamCreateFile(path, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
					if (currentStream == 0)
					{
						Console.WriteLine("[JUKEBOX] BASS failed to create stream for {0}", path);
					}

					else 
					{
						Console.WriteLine("[JUKEBOX] Current stream handle: {0}", currentStream);

						SYNCPROC end = new SYNCPROC(delegate (int handle, int channel, int data, IntPtr user)
							{
								// when the stream ends, go to the next stream in the playlist.
								Next();
							});

						Bass.BASS_ChannelSetSync(currentStream, BASSSync.BASS_SYNC_END, 0, end, System.IntPtr.Zero);
						Bass.BASS_ChannelPlay(currentStream, true);
						IsPlaying = true;
					}
				}

				// jukebox mode currently only plays songs, so skip if it's not a song.
				else Next();
			}
		}

		public List<MediaItem> ListOfSongs()
		{
			return playlist.ListOfMediaItems();
		}

		public void RemoveSongAtIndex(int index)
		{
			playlist.RemoveMediaItemAtIndex(index);
		}

		public void RemoveSongsAtIndexes(List<int> indices)
		{
			playlist.RemoveMediaItemAtIndexes(indices);
		}

		public void MoveSong(int fromIndex, int toIndex)
		{
			playlist.MoveMediaItem(fromIndex, toIndex);
		}

		public void AddSong(Song song)
		{
			playlist.AddMediaItem(song, true);
		}

		public void AddSongs(List<Song> songs)
		{
			var mediaItems = new List<MediaItem>();
			mediaItems.AddRange(songs);
			playlist.AddMediaItems(mediaItems);
		}

		public void InsertSong(Song song, int index)
		{
			playlist.InsertMediaItem(song, index);
		}

		public void ClearPlaylist()
		{
			playlist.ClearPlaylist();
		}

		private void BassInit()
		{
			// if we are initializing, we want to make sure that we're not
			// already initialized.
			if(IsInitialized) BassFree();

			// set the buffer to 200ms, which is the minimum.
			Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD + 200));
			Console.WriteLine("[JUKEBOX] BASS buffer size: {0}ms", Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_BUFFER));

			// dsp effects for floating polong math to avoid clipping
			Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_FLOATDSP, 1);

			// initialize the audio output device
			if(!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero))
			{
				Console.WriteLine("[JUKEBOX] Error initializing BASS!");
			}

			else IsInitialized = true;
			
		}

		private void BassFree()
		{
			Bass.BASS_Free();
			Bass.BASS_PluginFree(0);

			IsInitialized = false;
			IsPlaying = false;
			currentStream = 0;
		}

		private string BassErrorCodeToString(long code)
		{
			return "";
		}
	}
}
