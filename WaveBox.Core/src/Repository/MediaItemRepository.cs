using System;

namespace WaveBox.Core.Model.Repository {
    public class MediaItemRepository : IMediaItemRepository {
        private readonly IItemRepository itemRepository;
        private readonly ISongRepository songRepository;
        private readonly IVideoRepository videoRepository;

        public MediaItemRepository(IItemRepository itemRepository, ISongRepository songRepository, IVideoRepository videoRepository) {
            ArgumentNullException.ThrowIfNull(itemRepository);
            ArgumentNullException.ThrowIfNull(songRepository);
            ArgumentNullException.ThrowIfNull(videoRepository);

            this.itemRepository = itemRepository;
            this.songRepository = songRepository;
            this.videoRepository = videoRepository;
        }

        public IMediaItem MediaItemForId(int itemId) {
            IMediaItem item = null;
            ItemType type = itemRepository.ItemTypeForItemId(itemId);
            switch (type) {
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
