using System;
using System.Collections.Generic;
using WaveBox.Model;

namespace WaveBox.Model.Repository
{
	public interface IFolderRepository
	{
		List<Folder> MediaFolders();
		List<Folder> TopLevelFolders();

	}
}

