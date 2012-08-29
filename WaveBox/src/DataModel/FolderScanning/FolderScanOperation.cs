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

namespace WaveBox.DataModel.FolderScanning
{
    public class FolderScanOperation : AbstractOperation
    {
        public override string OperationType { get { return String.Format ("FolderScanOperation:{0}", FolderPath); } }

        private string folderPath;
        public string FolderPath { get { return folderPath; } }

		int testNumberOfFoldersInserted = 0;
		Stopwatch testFolderObjCreateTime = new Stopwatch();
		Stopwatch testGetDirectoriesTime = new Stopwatch();
		Stopwatch testMediaItemNeedsUpdatingTime = new Stopwatch();
		Stopwatch testIsExtensionValidTime = new Stopwatch();

        public FolderScanOperation(string path, int secondsDelay) : base(secondsDelay)
        {
            folderPath = path;
        }

        public override void Start()
        {
            ProcessFolder(FolderPath);

			Console.WriteLine("------------FOLDER SCAN --------------------");
			Console.WriteLine("folders inserted: " + testNumberOfFoldersInserted);
			Console.WriteLine("folder object create time: " + testFolderObjCreateTime.ElapsedMilliseconds + "ms");
			Console.WriteLine("get directories time: " + testGetDirectoriesTime.ElapsedMilliseconds + "ms");
			Console.WriteLine("media file needs updating time: " + testMediaItemNeedsUpdatingTime.ElapsedMilliseconds + "ms");
			Console.WriteLine("extension valid check time: " + testIsExtensionValidTime.ElapsedMilliseconds + "ms");
			long total = testFolderObjCreateTime.ElapsedMilliseconds + testGetDirectoriesTime.ElapsedMilliseconds + testMediaItemNeedsUpdatingTime.ElapsedMilliseconds + testIsExtensionValidTime.ElapsedMilliseconds;
			Console.WriteLine("total: " + total + "ms = " + total / 1000 + "s");
			Console.WriteLine("--------------------------------------------");
        }

        public void ProcessFolder(int folderId)
        {
			Folder folder = new Folder(folderId);
            ProcessFolder(folder.FolderPath);
        }

        public void ProcessFolder(string folderPath)
        {
            if (isRestart)
            {
                return;
            }

            try
            {
                // if the file is a directory
                if (Directory.Exists(folderPath))
                {
					testFolderObjCreateTime.Start();
                    Folder topFolder = new Folder(folderPath);
					testFolderObjCreateTime.Stop();
                    //Console.WriteLine("scanning " + topFolder.FolderName + "  id: " + topFolder.FolderId);

                    if (topFolder.FolderId == null)
                    {
						testNumberOfFoldersInserted++;
                        topFolder.InsertFolder(false);
                    }

					// Check the folder art
					string artPath = topFolder.ArtPath;
					if (Art.FileNeedsUpdating(artPath, topFolder.FolderId))
					{
						// Find the old art id, if it exists
						int? oldArtId = topFolder.ArtId;
						int? newArtId = new Art(artPath).ArtId;

						// Update any existing references, that would include both this folder
						// and any children that were using this art in lieu of embedded art
						Art.UpdateItemsToNewArtId(oldArtId, newArtId);
					}

                    //Stopwatch sw = new Stopwatch();
					testGetDirectoriesTime.Start();
					string[] directories = Directory.GetDirectories(folderPath);
					testGetDirectoriesTime.Stop();
                    foreach (string subfolder in directories)
                    {
                        if (!(subfolder.Contains(".AppleDouble")))
                        {
							testFolderObjCreateTime.Start();
                            Folder folder = new Folder(subfolder);
							testFolderObjCreateTime.Stop();

                            // if the folder isn't already in the database, add it.
                            if (folder.FolderId == null)
                            {
								testNumberOfFoldersInserted++;
                                folder.InsertFolder(false);
                            }
                            //sw.Start();
                            ProcessFolder(subfolder);
                            //Console.WriteLine("ProcessFolder ({0}) took {1}ms", subfolder, sw.ElapsedMilliseconds);
                            //sw.Reset();
                        }
                    }
                    //sw.Stop();

                    //sw.Start();
                    /*foreach (string currentFile in Directory.GetFiles(folderPath))
                    {
                    	ProcessFile(currentFile, topFolder.FolderId);
                    }*/

                    //sw.Stop();

                    //Parallel.ForEach(Directory.GetDirectories(topFile.FullName), currentFile =>
                    //    {
                    //        if (!(currentFile.Contains(".AppleDouble")))
                    //        {
                    //            Folder folder = new Folder(currentFile);
                    //
                    //            // if the folder isn't already in the database, add it.
                    //            if (folder.FolderId == 0)
                    //            {
                    //                folder.addToDatabase(false);
                    //            }
                    //            processFolder(currentFile);
                    //        }
                    //    });

                    Parallel.ForEach(Directory.GetFiles(folderPath), currentFile =>
                        {
                            ProcessFile(currentFile, topFolder.FolderId);
                        });
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("\t" + "[FOLDERSCAN(1)] \"" + folderPath + "\" : Directory does not exist. " + e.InnerException);
            }
            catch (DirectoryNotFoundException e)
            {
				Console.WriteLine("    ");
                Console.WriteLine("\t" + "[FOLDERSCAN(2)] \"" + folderPath + "\" : Directory does not exist. " + e.ToString());
            }
            catch (IOException e)
            {
                Console.WriteLine("\t" + "[FOLDERSCAN(3)] \"" + folderPath + "\" : " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("\t" + "[FOLDERSCAN(4)] \"" + folderPath + "\" : Error checking to see if the file was a directory: " + e.ToString());
            }
        }

        public void ProcessFile(string file, int? folderId)
		{
			if (isRestart)
			{
				return;
			}

			try
			{
				ItemType type = Item.ItemTypeForFilePath(file);

				if (type == ItemType.Song)
				{
					//Stopwatch sw = new Stopwatch();
					//sw.Start();
					testMediaItemNeedsUpdatingTime.Start();
					bool isNew = true;
					int? itemId = null;
					bool needsUpdating = MediaItem.FileNeedsUpdating(file, folderId, out isNew, out itemId);
					testMediaItemNeedsUpdatingTime.Stop();
					//Console.WriteLine("FileNeedsUpdating: {0} ms", sw.ElapsedMilliseconds);
					//sw.Reset();

					if (needsUpdating)
					{
						Console.WriteLine("[FOLDERSCAN] " + "File needs updating: " + file);
		                
						//sw.Start();
						TagLib.File f = null;
						try
						{
							f = TagLib.File.Create(file);
						}
						catch(TagLib.CorruptFileException e)
						{
							e.ToString();
							//Console.WriteLine("[FOLDERSCAN(5)] " + fileName + " has a corrupt tag and will not be inserted.");
							return;
						}
						catch(Exception e)
						{
							Console.WriteLine("[FOLDERSCAN(6)] " + "Error processing file: " + e.ToString());
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
							if (isNew)
							{
								new Song(file, folderId, f).InsertSong();
							}
							else if (itemId != null)
							{
								new Song((int)itemId).InsertSong();
							}
						}
					}
				}
			}
            catch (FileNotFoundException e)
            {
                Console.WriteLine("\t" + "[FOLDERSCAN(5)] \"" + file + "\" : Directory does not exist. " + e.InnerException);
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine("\t" + "[FOLDERSCAN(6)] \"" + file + "\" : Directory does not exist. " + e.InnerException);
            }
            catch (IOException e)
            {
                Console.WriteLine("\t" + "[FOLDERSCAN(7)] \"" + file + "\" : " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("\t" + "[FOLDERSCAN(8)] \"" + file + "\" : " + e.ToString());
            }
		}
	}
}

