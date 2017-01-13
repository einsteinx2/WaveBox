using System;

namespace WaveBox.Core.Model.Repository {
    public interface IMediaItemRepository {
        IMediaItem MediaItemForId(int itemId);
    }
}

