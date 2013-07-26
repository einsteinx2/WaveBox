using System;

namespace WaveBox.Core.Model.Repository
{
	public interface IArtRepository
	{
		Art ArtForId(int artId);
		int? ItemIdForArtId(int? artId);
		int? ArtIdForItemId(int? itemId);
		int? ArtIdForMd5(string hash);
		bool UpdateArtItemRelationship(int? artId, int? itemId, bool replace);
		bool RemoveArtRelationshipForItemId(int? itemId);
		bool UpdateItemsToNewArtId(int? oldArtId, int? newArtId);
	}
}

