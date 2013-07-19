using System;

namespace WaveBox.Model.Repository
{
	public interface IItemRepository
	{
		int? GenerateItemId(ItemType itemType);
		ItemType ItemTypeForItemId(int itemId);
		ItemType ItemTypeForFilePath(string filePath);
	}
}

