using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaFerry.DataModel.Model;
using MediaFerry.DataModel.Singletons;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Diagnostics;
using TagLib;

namespace MediaFerry.DataModel.FolderScanning
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

					foreach (var subfolder in Directory.GetDirectories(topFile.FullName))
					{
						if (!(subfolder.Contains(".AppleDouble")))
						{
							var folder = new Folder(subfolder);

							// if the folder isn't already in the database, add it.
							if (folder.FolderId == 0)
							{
								folder.addToDatabase(false);
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
					//                folder.addToDatabase(false);
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

			catch (FileNotFoundException e)
			{
				Console.WriteLine("\t" + folderPath + ": Directory does not exist. " + e.InnerException);
			}

			catch (DirectoryNotFoundException e)
			{
				Console.WriteLine("\t" + folderPath + ": Directory does not exist. " + e.InnerException);
			}
			catch (IOException e)
			{
				Console.WriteLine("\t" + folderPath + ": " + e.Message);
			}
			catch (Exception e)
			{
				Console.WriteLine("\t" + "Error checking to see if the file was a directory: " + e.ToString());
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
						Console.WriteLine(file.Name + " has a corrupt tag and will not be inserted.");
						return;
					}

					catch (Exception e)
					{
						Console.WriteLine("Error processing file: " + e.ToString());
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

		}
	}


