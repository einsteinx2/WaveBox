using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Diagnostics;
using TagLib;

namespace WaveBox.DataModel.FolderScanning
{
	class FolderScanOperation : ScanOperation
	{
		private string folderPath;
		public string FolderPath
		{
			get
			{
				return folderPath;
			}
		}

		private string[] validExtensions = { "mp3", "m4a", "mp4", "flac", "wv", "mpc", "ogg", "wma" };
		private List<string> validExtensionsList;

		public FolderScanOperation(string path, int secondsDelay) : base(secondsDelay)
		{
			validExtensionsList = new List<string>(validExtensions);
			folderPath = path;
		}

		public override void Start()
		{
			ProcessFolder(FolderPath);
		}

		public void ProcessFolder(int folderId)
		{
			var folder = new Folder(folderId);
			ProcessFolder(folder.FolderPath);
		}

		public void ProcessFolder(string folderPath)
		{
			if (ShouldRestart)
			{
				return;
			}

			FileInfo topFile;

			try
			{
				topFile = new FileInfo(folderPath);

				if (topFile.Directory.Exists == false)
				{
					return;
				}

				Folder topFolder;

				// if the file is a directory
				if ((topFile.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
				{
					topFolder = new Folder(topFile.FullName);
					//Console.WriteLine("scanning " + topFolder.FolderName + "  id: " + topFolder.FolderId);

					if (topFolder.FolderId == 0)
					{
						topFolder.AddToDatabase(false);
					}

					foreach (var subfolder in Directory.GetDirectories(topFile.FullName))
					{
						if (!(subfolder.Contains(".AppleDouble")))
						{
							var folder = new Folder(subfolder);

							// if the folder isn't already in the database, add it.
							if (folder.FolderId == 0)
							{
								folder.AddToDatabase(false);
							}
							ProcessFolder(subfolder);
						}
					}

					foreach (var subfile in Directory.GetFiles(topFile.FullName))
					{
						// if the subfile is a directory...
						ProcessFile(new FileInfo(subfile), topFolder.FolderId);
						//Console.WriteLine(subfile);
					}

					//Parallel.ForEach(Directory.GetDirectories(topFile.FullName), currentFile =>
					//    {
					//        if (!(currentFile.Contains(".AppleDouble")))
					//        {
					//            var folder = new Folder(currentFile);

					//            // if the folder isn't already in the database, add it.
					//            if (folder.FolderId == 0)
					//            {
					//                folder.addToDatabase(false);
					//            }
					//            processFolder(currentFile);
					//        }
					//    });

					//Parallel.ForEach(Directory.GetFiles(topFile.FullName), currentFile =>
					//    {
					//        processFile(new FileInfo(currentFile), topFolder.FolderId);
					//    });



				}
			}

			catch (FileNotFoundException e)
			{
				Console.WriteLine("\t" + "[FOLDERSCAN] " + folderPath + ": Directory does not exist. " + e.InnerException);
			}

			catch (DirectoryNotFoundException e)
			{
				Console.WriteLine("\t" + "[FOLDERSCAN] " + folderPath + ": Directory does not exist. " + e.InnerException);
			}
			catch (IOException e)
			{
				Console.WriteLine("\t" + "[FOLDERSCAN] " + folderPath + ": " + e.Message);
			}
			catch (Exception e)
			{
				Console.WriteLine("\t" + "[FOLDERSCAN] " + "Error checking to see if the file was a directory: " + e.ToString());
			}
		}

		public void ProcessFile(FileInfo file, int folderId)
		{
			if (ShouldRestart)
			{
				return;
			}

			// make sure the extension is valid
			if (!validExtensionsList.Contains(file.Extension.Substring(1).ToLower()))
			{
				return;
			}


			if (MediaItem.FileNeedsUpdating(file))
			{
				Console.WriteLine("[FOLDERSCAN] " + "File needs updating: " + file.Name);
				var sw = new Stopwatch();
				sw.Start();
				TagLib.File f = null;
				try
				{
					f = TagLib.File.Create(file.FullName);
				}

				catch (TagLib.CorruptFileException e)
				{
					e.ToString();
					Console.WriteLine("[FOLDERSCAN] " + file.Name + " has a corrupt tag and will not be inserted.");
					return;
				}

				catch (Exception e)
				{
					Console.WriteLine("[FOLDERSCAN] " + "Error processing file: " + e.ToString());
				}
				sw.Stop();
				//Console.WriteLine("Get tag: {0} ms", sw.ElapsedMilliseconds);

				sw.Reset();

				if (f == null)
				{
					// Must be something not supported by TagLib-Sharp
				}

				else
				{
					// It's a song!  Do yo thang.
					sw.Start();
					var song = new Song(file, folderId);
					sw.Stop();
					//Console.WriteLine("Create new song object: {0} ms", sw.ElapsedMilliseconds);

					sw.Reset();
					sw.Start();
					song.updateDatabase();
					sw.Stop();
					//Console.WriteLine("Update database: {0} ms", sw.ElapsedMilliseconds);
				}
			}
		}

		public override string ScanType()
		{
			return String.Format("FolderScanOperation:{0}", FolderPath);
		}
	}
}


