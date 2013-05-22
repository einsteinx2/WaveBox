using System;
using NUnit.Framework;
using WaveBox.Model;

namespace WaveBoxText
{
	// Example NUnit test
	[TestFixture]
	public class FolderTest
	{
		[Test]
		public void CompareFolderByName()
		{
			int result = Folder.CompareFolderByName(new Folder.Factory().CreateFolder(1), new Folder.Factory().CreateFolder(1));
			Assert.AreEqual(0, result);
		}
	}
}

