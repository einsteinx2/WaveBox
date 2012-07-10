using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaFerry.DataModel.Model;
using Un4seen.Bass;

namespace MediaFerry.DataModel.Singletons
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

		private bool _isInitialized;
		public bool IsInitialized
		{
			get
			{
				return _isInitialized;
			}
			set
			{
				_isInitialized = value;
			}
		}

		private bool _isPlaying;
		public bool IsPlaying
		{
			get
			{
				return _isPlaying;
			}
			set
			{
				_isPlaying = value;
			}
		}

		private int _currentIndex;
		public int CurrentIndex
		{
			get
			{
				return _currentIndex;
			}
			set
			{
				_currentIndex = value;
			}
		}

		private const string _PLAYLIST_NAME = "jukeboxQPbjnbh2JPU5NGxhXiiQ";
		private static Playlist _playlist;

		private int _currentStream;

		private Jukebox()
		{
			_playlist = new Playlist(_PLAYLIST_NAME);
			if(_playlist.PlaylistId == 0)
			{
				// playlist doesn't exist, so create it.
				_playlist.createPlaylist();
			}
		}

		public double Progress()
		{
			if (IsInitialized && _currentStream != 0)
			{
				long bytePosition = Bass.BASS_ChannelGetPosition(_currentStream, BASSMode.BASS_POS_BYTES | BASSMode.BASS_POS_DECODE);
				double seconds = Bass.BASS_ChannelBytes2Seconds(_currentStream, bytePosition);
				return seconds;
			}

			return 0.0;
		}

		public void Play()
		{
			if (_currentStream != 0)
			{
				Bass.BASS_Start();
				_isPlaying = true;
			}
		}

		public void Pause()
		{
			if (IsPlaying && _currentStream != 0)
			{
				Bass.BASS_Pause();
				IsPlaying = false;
			}
		}

		public void Stop()
		{
			if (IsInitialized)
			{
				_bassFree();
			}
		}

		public void Prev()
		{
			CurrentIndex = CurrentIndex - 1 < 0 ? 0 : CurrentIndex - 1;
			PlaySongAtIndex(CurrentIndex);
		}

		public void Next()
		{
			CurrentIndex = CurrentIndex + 1;

			if (CurrentIndex >= _playlist.PlaylistCount)
			{
				CurrentIndex = CurrentIndex - 1;
				Stop();
			}
			else PlaySongAtIndex(CurrentIndex);
		}

		public void PlaySongAtIndex(int index)
		{
			MediaItem item = _playlist.mediaItemAtIndex(index);
			Console.WriteLine("[JUKEBOX] Playing song: " + item.FileName);

			if(item != null)
			{
				if(item.ItemTypeId == (int)ItemType.SONG)
				{
					// set the current index
					CurrentIndex = index;

					// re-initialize bass
					_bassInit();

					// create the stream
					string path = item.file().Name;
					_currentStream = Bass.BASS_StreamCreateFile(path, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
					if(_currentStream == 0)
					{
						Console.WriteLine("[JUKEBOX] BASS failed to create stream for {0}", path);
					}

					else 
					{
						Console.WriteLine("[JUKEBOX] Current stream handle: {0}", _currentStream);

						SYNCPROC end = new SYNCPROC(delegate (int handle, int channel, int data, IntPtr user)
							{
								// when the stream ends, go to the next stream in the playlist.
								Next();
							});

						Bass.BASS_ChannelSetSync(_currentStream, BASSSync.BASS_SYNC_END, 0, end, System.IntPtr.Zero);
						Bass.BASS_ChannelPlay(_currentStream, true);
						IsPlaying = true;
					}
				}

				// jukebox mode currently only plays songs, so skip if it's not a song.
				else Next();
			}
		}

		public List<MediaItem> ListOfSongs()
		{
			return _playlist.listOfMediaItems();
		}

		public void RemoveSongAtIndex(int index)
		{
			_playlist.removeMediaItemAtIndex(index);
		}

		public void RemoveSongsAtIndexes(List<int> indices)
		{
			_playlist.removeMediaItemAtIndexes(indices);
		}

		public void MoveSong(int fromIndex, int toIndex)
		{
			_playlist.moveMediaItem(fromIndex, toIndex);
		}

		public void AddSong(Song song)
		{
			_playlist.addMediaItem(song, true);
		}

		public void AddSongs(List<Song> songs)
		{
			var mediaItems = new List<MediaItem>();
			mediaItems.AddRange(songs);
			_playlist.addMediaItems(mediaItems);
		}

		public void InsertSong(Song song, int index)
		{
			_playlist.insertMediaItem(song, index);
		}

		public void ClearPlaylist()
		{
			_playlist.clearPlaylist();
		}

		private void _bassInit()
		{
			// if we are initializing, we want to make sure that we're not
			// already initialized.
			if(IsInitialized) _bassFree();

			// set the buffer to 200ms, which is the minimum.
			Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD + 200));
			Console.WriteLine("[JUKEBOX] BASS buffer size: {0}ms", Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_BUFFER));

			// dsp effects for floating point math to avoid clipping
			Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_FLOATDSP, 1);

			// initialize the audio output device
			if(!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero))
			{
				Console.WriteLine("[JUKEBOX] Error initializing BASS!");
			}

			else IsInitialized = true;
			
		}

		private void _bassFree()
		{
			Bass.BASS_Free();
			Bass.BASS_PluginFree(0);

			_isInitialized = false;
			_isPlaying = false;
			_currentStream = 0;
		}

		private string _bassErrorCodeToString(int code)
		{
			return "";
		}
	}
}
