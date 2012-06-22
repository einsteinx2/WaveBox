using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace pms.DataModel.Model
{
	public class MediaItem
	{
		public int ItemTypeId
		{
			get
			{
				return 0;
			}
		}

		protected MediaItemType _mediaItemType;
		public MediaItemType MediaItemType
		{
			get
			{
				return _mediaItemType;
			}

			set
			{
				_mediaItemType = value;
			}
		}

		protected int _itemId;
		public int ItemId
		{
			get
			{
				return _itemId;
			}

			set
			{
				_itemId = value;
			}
		}

		protected int _artId;
		public int ArtId
		{
			get
			{
				return _artId;
			}

			set
			{
				_artId = value;
			}
		}

		protected int _folderId;
		public int FolderId
		{
			get
			{
				return _folderId;
			}

			set
			{
				_folderId = value;
			}
		}

		protected FileType _fileType;
		public FileType FileType
		{
			get
			{
				return _fileType;
			}

			set
			{
				_fileType = value;
			}
		}

		protected int _duration;
		public int Duration
		{
			get
			{
				return _duration;
			}

			set
			{
				_duration = value;
			}
		}

		protected long _bitrate;
		public long Bitrate
		{
			get
			{
				return _bitrate;
			}

			set
			{
				_bitrate = value;
			}
		}

		protected long _fileSize;
		public long FileSize
		{
			get
			{
				return _fileSize;
			}

			set
			{
				_fileSize = value;
			}
		}

		protected long _lastModified;
		public long LastModified
		{
			get
			{
				return _lastModified;
			}

			set
			{
				_lastModified = value;
			}
		}

		protected string _fileName;
		public string FileName
		{
			get
			{
				return _fileName;
			}

			set
			{
				_fileName = value;
			}
		}

		/// <summary>
		/// Public methods
		/// </summary>

		public void addToPlaylist(Playlist thePlaylist, int index)
		{
		}

		public static bool fileNeedsUpdating(FileStream file)
		{
			return true;
		}
	}
}
