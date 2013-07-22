using System;

namespace WaveBox.Model.Repository
{
	public interface IMediaItemRepository
	{
		IMediaItem MediaItemForId(int itemId);
	}
}

