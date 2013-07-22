using System;

namespace WaveBox.Model.Repository
{
	public class MediaItemRepository : IMediaItemRepository
	{
		private readonly IItemRepository itemRepository;
		private readonly ISongRepository songRepository;
		private readonly IVideoRepository videoRepository;

		public MediaItemRepository(IItemRepository itemRepository, ISongRepository songRepository, IVideoRepository videoRepository)
		{
			if (itemRepository == null)
				throw new ArgumentNullException("itemRepository");
			if (songRepository == null)
				throw new ArgumentNullException("songRepository");
			if (videoRepository == null)
				throw new ArgumentNullException("videoRepository");

			this.itemRepository = itemRepository;
			this.songRepository = songRepository;
			this.videoRepository = videoRepository;
		}

		public IMediaItem MediaItemForId(int itemId)
		{
			IMediaItem item = null;
			ItemType type = itemRepository.ItemTypeForItemId(itemId);
			switch (type)
			{
				case ItemType.Song:
					item = songRepository.SongForId(itemId);
					break;
					case ItemType.Video:
					item = videoRepository.VideoForId(itemId);
					break;
			}

			return item;
		}
	}
}

