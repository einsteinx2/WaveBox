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
using WaveBox.OperationQueue;
using TagLib;
using System.Security;

namespace WaveBox.DataModel.FolderScanning
{
	public class FolderScanOperation : AbstractOperation
	{
		public override string OperationType { get { return String.Format ("FolderScanOperation:{0}", FolderPath); } }

		private string folderPath;
		public string FolderPath { get { return folderPath; } }

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
			if (isRestart || folderPath == null)
			{
				return;
			}

			try
			{
				// Must be a valid directory
				if (!Directory.Exists(folderPath))
				{
					return;
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("\t" + "[FOLDERSCAN(1)] " + folderPath + ": Error checking if Directory exists. " + e.InnerException);
			}

			// Queue to hold folder names to process
			Queue<DirectoryInfo> processQueue = new Queue<DirectoryInfo>();

			// Access the top directory's information
			DirectoryInfo topDirectory = null;
			try
			{
				topDirectory = new DirectoryInfo(folderPath);
			}
			catch(SecurityException e)
			{
				Console.WriteLine("\t" + "[FOLDERSCAN(2)] " + folderPath + ": Do not have permission to access directory. " + e.InnerException);
			}
			catch(ArgumentException e)
			{
				Console.WriteLine("\t" + "[FOLDERSCAN(3)] " + folderPath + ": Invalid characters in path. " + e.InnerException);
			}
			catch(PathTooLongException e)
			{
				Console.WriteLine("\t" + "[FOLDERSCAN(4)] " + folderPath + ": Path is too long. " + e.InnerException);
			}

			// Enqueue the top directory to start processing
			if (topDirectory != null)
			{
				processQueue.Enqueue(topDirectory);
			}

			while(processQueue.Count > 0)
			{
				try
				{
					DirectoryInfo directory = processQueue.Dequeue();
					//Console.WriteLine("\t" + "processQueue count: " + processQueue.Count + " directory: " + directory.Name);
					Folder folder = new Folder(directory.FullName);
					if (folder.FolderId == null)
					{
						folder.InsertFolder(false);
					}

					// Process any files this folder contains
					Parallel.ForEach(directory.EnumerateFiles(), fileInfo =>
				    {
				        ProcessFile(fileInfo, folder.FolderId);
				    });

					// Queue up any subdirectories
					foreach(DirectoryInfo subDir in directory.EnumerateDirectories())
					{
						if (subDir.Name != ".AppleDouble")
						{
							processQueue.Enqueue(subDir);
						}
					}
				}
				catch(Exception e)
				{
					Console.WriteLine("\t" + "[FOLDERSCAN(5)] " + folderPath + ": Error processing directory. " + e.InnerException);
				}

				// Garbage collect before continuing
				GC.Collect();
			}
		}

		public void ProcessFile(FileInfo file, int? folderId)
		{
			if (isRestart)
			{
				return;
			}

			// make sure the extension is valid
			if (!validExtensionsList.Contains(file.Extension.Substring(1).ToLower()))
			{
				return;
			}

            //var sw = new Stopwatch();
            //sw.Start();
            bool needsUpdating = MediaItem.FileNeedsUpdating(file, folderId);
			//Console.WriteLine("Processing file: " + file.Name);
            //sw.Reset();

			if (needsUpdating)
			{
				Console.WriteLine("[FOLDERSCAN(6)] " + "File needs updating: " + file.Name);
				
				//sw.Start();
				TagLib.File f = null;
				try
				{
					f = TagLib.File.Create(file.FullName);
				}

				catch (TagLib.CorruptFileException e)
				{
					e.ToString();
					Console.WriteLine("[FOLDERSCAN(7)] " + file.Name + " has a corrupt tag and will not be inserted.");
					return;
				}

				catch (Exception e)
				{
					Console.WriteLine("[FOLDERSCAN(8)] " + "Error processing file: " + e.ToString());
				}
				//Console.WriteLine("Get tag: {0} ms", sw.ElapsedMilliseconds);

				//sw.Reset();

				if (f == null)
				{
					// Must be something not supported by TagLib-Sharp
				}

				else
				{
					// It's a song!  Do yo thang.
				//	sw.Start();
					var song = new Song(file, folderId, f);
				//	Console.WriteLine("Create new song object: {0} ms", sw.ElapsedMilliseconds);
				//	sw.Restart();

					song.InsertSong();
				//	sw.Stop();
				//	Console.WriteLine("Update database: {0} ms", sw.ElapsedMilliseconds);
				}
			}
		}
	}
}


