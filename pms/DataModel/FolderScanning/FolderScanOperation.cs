using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pms.DataModel.Model;
using pms.DataModel.Singletons;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using TagLib;

namespace pms.DataModel.FolderScanning
{
	class FolderScanOperation : ScanOperation
	{
		private string _folderPath;
		public string FolderPath
		{
			get
			{
				return _folderPath;
			}
		}

		private string[] _validExtensions = { "mp3", "aac", "m4a", "mp4", "flac", "wv", "mpc", "ogg" };
		private List<string> _validExtensionsList;

		public FolderScanOperation(string folderPath, int secondsDelay) : base(secondsDelay)
		{
			_validExtensionsList = new List<string>(_validExtensions);
			_folderPath = folderPath;
		}

		public void start()
		{
			processFolder(FolderPath);
		}

		public void processFolder(int folderId)
		{
			var folder = new Folder(folderId);
			processFolder(folder.FolderPath);
		}

		public void processFolder(string folderPath)
		{
			if (isRestart())
			{
				return;
			}

			var topFile = new FileInfo(folderPath);

			try
			{
				Folder topFolder;

				// if the file is a directory
				if ((topFile.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
				{
					topFolder = new Folder(topFile.FullName);
					Console.WriteLine("scanning " + topFolder.FolderName + "  id: " + topFolder.FolderId);

					foreach (var subfolder in Directory.GetDirectories(topFile.FullName))
					{
						if (!(subfolder.Contains(".AppleDouble")))
						{
							var folder = new Folder(subfolder);

							// if the folder isn't already in the database, add it.
							if (folder.FolderId == 0)
							{
								folder.addToDatabase();
							}
							processFolder(subfolder);
						}
					}

					//Parallel.ForEach(Directory.GetDirectories(topFile.FullName), currentFile =>
					//    {
					//        if (!(currentFile.Contains(".AppleDouble")))
					//        {
					//            var folder = new Folder(currentFile);

					//            // if the folder isn't already in the database, add it.
					//            if (folder.FolderId == 0)
					//            {
					//                folder.addToDatabase();
					//            }
					//            processFolder(currentFile);
					//        }
					//    });
					foreach (var subfile in Directory.GetFiles(topFile.FullName))
					{
						// if the subfile is a directory...
						processFile(new FileInfo(subfile), topFolder.FolderId);
						//Console.WriteLine(subfile);
					}

					//Parallel.ForEach(Directory.GetFiles(topFile.FullName), currentFile =>
					//    {
					//        processFile(new FileInfo(currentFile), topFolder.FolderId);
					//    });


					
				}
			}

			catch (DirectoryNotFoundException e)
			{
				Console.WriteLine("Directory does not exist. " + e.InnerException);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error checking to see if the file was a directory: " + e.ToString());
			}
		}

			public void processFile(FileInfo file, int folderId)
			{
				if (isRestart())
				{
					return;
				}

				// make sure the extension is valid
				if (!_validExtensionsList.Contains(file.Extension.Substring(1).ToLower()))
				{
					return;
				}

				if (MediaItem.fileNeedsUpdating(file))
				{
					Console.WriteLine("File needs updating: " + file.Name);
					TagLib.File f = null;
					try
					{
						f = TagLib.File.Create(file.FullName);
					}
					catch (Exception e)
					{
						Console.WriteLine("Error processing file: " + e.ToString());
					}

					if (f == null)
					{
						// Must be something not supported by TagLib-Sharp
					}

					else
					{
						// It's a song!  Do yo thang.
						var song = new Song(file, folderId);
						song.updateDatabase();
					}
				}
			}

		}
	}


