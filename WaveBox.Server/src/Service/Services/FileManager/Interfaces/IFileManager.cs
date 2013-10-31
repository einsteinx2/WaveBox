using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.OperationQueue;
using WaveBox.FolderScanning;
using WaveBox.Service;
using WaveBox.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Service.Services.FileManager
{
	public interface IFileManager
	{
		bool Start();
		bool Stop();

		void OnChanged(object source, FileSystemEventArgs e);
		void OnCreated(object source, FileSystemEventArgs e);
		void OnDeleted(object source, FileSystemEventArgs e);
		void OnRenamed(object source, RenamedEventArgs e);

		void ItemCreated(string fullPath);
		void ItemDeleted();
		void ItemRenamed(string fullPath);
	}
}
