﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using TagLib;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.OperationQueue;
using WaveBox.Static;

namespace WaveBox.FolderScanning
{
	public class FolderScanOperation : AbstractOperation
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override string OperationType { get { return String.Format("FolderScanOperation: {0}", FolderPath); } }

		private string folderPath;
		public string FolderPath { get { return folderPath; } }

		int testNumberOfFoldersInserted = 0;
		Stopwatch testTotalScanTime = new Stopwatch();
		Stopwatch testFolderObjCreateTime = new Stopwatch();
		Stopwatch testGetDirectoriesTime = new Stopwatch();
		Stopwatch testMediaItemNeedsUpdatingTime = new Stopwatch();
		Stopwatch testIsExtensionValidTime = new Stopwatch();

		public FolderScanOperation(string path, int delayMilliSeconds) : base(delayMilliSeconds)
		{
			folderPath = path;
		}

		public override void Start()
		{
			testTotalScanTime.Start();
			this.ProcessFolder(FolderPath);
			testTotalScanTime.Stop();

			logger.IfInfo("---------------- FOLDER SCAN ----------------");
			logger.IfInfo("total scan time: " + testTotalScanTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("---------------------------------------------");
			logger.IfInfo("folders inserted: " + testNumberOfFoldersInserted);
			logger.IfInfo("folder object create time: " + testFolderObjCreateTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("get directories time: " + testGetDirectoriesTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("media file needs updating time: " + testMediaItemNeedsUpdatingTime.ElapsedMilliseconds + "ms");
			logger.IfInfo("extension valid check time: " + testIsExtensionValidTime.ElapsedMilliseconds + "ms");
			long total = testFolderObjCreateTime.ElapsedMilliseconds + testGetDirectoriesTime.ElapsedMilliseconds + testMediaItemNeedsUpdatingTime.ElapsedMilliseconds + testIsExtensionValidTime.ElapsedMilliseconds;
			logger.IfInfo("total: " + total + "ms = " + total / 1000 + "s");
			logger.IfInfo("---------------------------------------------");
		}

		public void ProcessFolder(int folderId)
		{
			Folder folder = Injection.Kernel.Get<IFolderRepository>().FolderForId(folderId);
			this.ProcessFolder(folder.FolderPath);
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
					Folder topFolder = Injection.Kernel.Get<IFolderRepository>().FolderForPath(folderPath);
					testFolderObjCreateTime.Stop();

					if (topFolder.FolderId == null)
					{
						testNumberOfFoldersInserted++;
						topFolder.InsertFolder(false);
					}

					testGetDirectoriesTime.Start();
					string[] directories = Directory.GetDirectories(folderPath);
					testGetDirectoriesTime.Stop();
					foreach (string subfolder in directories)
					{
						if (!subfolder.Contains(".AppleDouble"))
						{
							testFolderObjCreateTime.Start();
							Folder folder = Injection.Kernel.Get<IFolderRepository>().FolderForPath(subfolder);
							testFolderObjCreateTime.Stop();

							// if the folder isn't already in the database, add it.
							if (folder.FolderId == null)
							{
								testNumberOfFoldersInserted++;
								folder.InsertFolder(false);
							}

							ProcessFolder(subfolder);
						}
					}

					// NOTE: File processing was previously using Parallel.ForEach, but it has been changed to be
					// single-threaded because:
					//   1) Parallel scanning has the potential to cause race conditions on this code
					//   2) Thanks to using "PRAGMA synchronous = OFF" in SQLite, scanning is much faster
					//   3) Single-threaded scanning may be faster on HDDs, due to less thrashing of disk as the disk
					//      seeks to the next file
					// In the future, we may revisit the issue, and perhaps detect if media lies on a SSD, allowing
					// us to benefit more from parallel scanning.
					foreach (string file in Directory.GetFiles(folderPath))
					{
						ProcessFile(file, topFolder.FolderId);
					}
				}
			}
			catch (FileNotFoundException e)
			{
				logger.Error("\"" + folderPath + "\" : Directory does not exist. " + e);
			}
			catch (DirectoryNotFoundException e)
			{
				logger.Error("\"" + folderPath + "\" : Directory does not exist. " + e);
			}
			catch (IOException e)
			{
				logger.Error("\"" + folderPath + "\" : " + e);
			}
			catch (UnauthorizedAccessException e)
			{
				logger.Error("\"" + folderPath + "\" : Access denied. " + e);
			}
			catch (Exception e)
			{
				logger.Error("\"" + folderPath + "\" : Error checking to see if the file was a directory: " + e);
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
				ItemType type = Injection.Kernel.Get<IItemRepository>().ItemTypeForFilePath(file);

				if (type == ItemType.Song || type == ItemType.Video)
				{
					testMediaItemNeedsUpdatingTime.Start();
					bool isNew = true;
					int? itemId = null;
					bool needsUpdating = FileNeedsUpdating(file, folderId, out isNew, out itemId);
					testMediaItemNeedsUpdatingTime.Stop();

					if (needsUpdating)
					{
						logger.IfInfo("Updating: " + file);

						TagLib.File f = null;
						try
						{
							f = TagLib.File.Create(file);
						}
						catch (Exception e)
						{
							// Tag is invalid in some way
							if (e is TagLib.CorruptFileException || e is ArgumentOutOfRangeException)
							{
								logger.Error(file + " has a corrupt tag and will not be inserted.");
								return;
							}
							else
							{
								// Other exceptions
								logger.Error("Error processing file " + file + ": " + e);
								return;
							}
						}

						if (f == null)
						{
							// Must be something not supported by TagLib-Sharp
							logger.IfInfo(file + " is not supported by taglib and will not be inserted.");
						}
						else
						{
							if (type == ItemType.Song)
							{
								// It's a song!  Do yo thang.
								if (isNew)
								{
									CreateSong(file, folderId, f).InsertMediaItem();
								}
								else if (itemId != null)
								{
									var oldSong = Injection.Kernel.Get<ISongRepository>().SongForId((int)itemId);
									var newSong = CreateSong(file, folderId, f);
									newSong.ItemId = oldSong.ItemId;
									newSong.InsertMediaItem();
								}
							}
							else if (type == ItemType.Video)
							{
								if (isNew)
								{
									CreateVideo(file, folderId, f).InsertMediaItem();
								}
								else if (itemId != null)
								{
									Injection.Kernel.Get<IVideoRepository>().VideoForId((int)itemId).InsertMediaItem();
								}
							}
						}

						// Dispose of file as it is no longer needed
						f.Dispose();
					}
				}
				else if (type == ItemType.Art)
				{
					if (ArtFileNeedsUpdating(file))
					{
						Folder folder = Injection.Kernel.Get<IFolderRepository>().FolderForId((int)folderId);
						IList<Album> albumsForFolder = Injection.Kernel.Get<IFolderRepository>().AlbumsForFolderId((int)folderId);

						// Find the old art id, if it exists
						int? oldArtId = folder.ArtId;
						int? newArtId = CreateArt(file).ArtId;

						if ((object)oldArtId == null)
						{
							logger.IfInfo("Adding new art for folderId: " + folderId);

							// Insert the relationship
							Injection.Kernel.Get<IArtRepository>().UpdateArtItemRelationship(newArtId, folderId, true);

							// Update all matching albums, but only if they don't already have art (prefer song embedded for albums)
							foreach (Album album in albumsForFolder)
							{
								// Insert the relationship for the album
								Injection.Kernel.Get<IArtRepository>().UpdateArtItemRelationship(newArtId, album.AlbumId, false);
							}
						}
						else
						{
							Art oldArt = Injection.Kernel.Get<IArtRepository>().ArtForId((int)oldArtId);

							// Check if the previous folder art was actually from embedded tag art
							if ((object)oldArt.FilePath == null)
							{
								// This was embedded tag art, so only update the folder's relationship
								bool updated = Injection.Kernel.Get<IArtRepository>().UpdateArtItemRelationship(newArtId, folder.FolderId, true);
								logger.IfInfo(String.Format("It was embedded art, {0}, newArtId: {1}, folderId: {2}", updated, newArtId, folder.FolderId));

								// Update all matching albums, but only if they don't already have art (prefer song embedded for albums)
								foreach (Album album in albumsForFolder)
								{
									// Insert the relationship for the album
									Injection.Kernel.Get<IArtRepository>().UpdateArtItemRelationship(newArtId, album.AlbumId, false);
								}
							}
							else
							{
								// Update any existing references, that would include both this folder
								// and any songs or albums that were using this art in lieu of embedded art
								Injection.Kernel.Get<IArtRepository>().UpdateItemsToNewArtId(oldArtId, newArtId);
							}
						}

						// Add this art to any media items in this folder which have no art.
						var items = folder.ListOfMediaItems();

						foreach (MediaItem m in items)
						{
							if (m.ArtId == null)
							{
								logger.IfInfo("Updating art id for item " + m.ItemId + ". (" + (m.ArtId == null ? "null" : m.ArtId.ToString()) + " -> " + newArtId + ")");
								Injection.Kernel.Get<IArtRepository>().UpdateArtItemRelationship(newArtId, m.ItemId, false);
							}
						}

						logger.IfInfo("Updating art for folderId: " + folderId);
					}
				}
			}
			catch (FileNotFoundException e)
			{
				logger.Error("\"" + file + "\" : File does not exist. " + e);
			}
			catch (DirectoryNotFoundException e)
			{
				logger.Error("\"" + file + "\" : Directory does not exist. " + e);
			}
			catch (IOException e)
			{
				logger.Error("\"" + file + "\" : IO error. " + e);
			}
			catch (UnauthorizedAccessException e)
			{
				logger.Error("\"" + file + "\" : Access denied. " + e);
			}
			catch (Exception e)
			{
				logger.Error("\"" + file + "\" : " + e);
			}
		}

		public Song CreateSong(string filePath, int? folderId, TagLib.File file)
		{
			int? itemId = Injection.Kernel.Get<IItemRepository>().GenerateItemId(ItemType.Song);
			if (itemId == null)
			{
				return new Song();
			}

			Song song = new Song();
			song.ItemId = itemId;
			song.FolderId = folderId;

			// Parse taglib tags
			TagLib.Tag tag = file.Tag;

			try
			{
				string firstPerformer = tag.FirstPerformer;
				if (firstPerformer != null)
				{
					Artist artist = Injection.Kernel.Get<IArtistRepository>().ArtistForNameOrCreate(firstPerformer.Trim());
					song.ArtistId = artist.ArtistId;
					song.ArtistName = artist.ArtistName;
				}
			}
			catch (Exception e)
			{
				if (logger.IsErrorEnabled) logger.Error("Error creating artist info for song: ", e);
				song.ArtistId = null;
				song.ArtistName = null;
			}

			try
			{
				string firstAlbumArtist = tag.FirstAlbumArtist;
				if (firstAlbumArtist != null)
				{
					AlbumArtist albumArtist = Injection.Kernel.Get<IAlbumArtistRepository>().AlbumArtistForNameOrCreate(firstAlbumArtist.Trim());
					song.AlbumArtistId = albumArtist.AlbumArtistId;
					song.AlbumArtistName = albumArtist.AlbumArtistName;
				}
			}
			catch (Exception e)
			{
				if (logger.IsErrorEnabled) logger.Error("Error creating album artist info for song: ", e);
				song.AlbumArtistId = null;
				song.AlbumArtistName = null;
			}

			// If we have an artist, but not an albumArtist, then use the artist info for albumArtist
			if (song.AlbumArtistId == null && song.ArtistName != null)
			{
				AlbumArtist albumArtist = Injection.Kernel.Get<IAlbumArtistRepository>().AlbumArtistForNameOrCreate(song.ArtistName);
				song.AlbumArtistId = albumArtist.AlbumArtistId;
				song.AlbumArtistName = albumArtist.AlbumArtistName;
			}

			try
			{
				string albumName = tag.Album;
				if (albumName != null)
				{
					Album album = Injection.Kernel.Get<IAlbumRepository>().AlbumForName(albumName.Trim(), song.AlbumArtistId, Convert.ToInt32(tag.Year));
					song.AlbumId = album.AlbumId;
					song.AlbumName = album.AlbumName;
					song.ReleaseYear = album.ReleaseYear;
				}
			}
			catch (Exception e)
			{
				if (logger.IsErrorEnabled) logger.Error("Error creating album info for song: ", e);
				song.AlbumId = null;
				song.AlbumName = null;
				song.ReleaseYear = null;
			}

			song.FileType = song.FileType.FileTypeForTagLibMimeType(file.MimeType);

			if (song.FileType == FileType.Unknown)
			{
				logger.IfInfo("\"" + filePath + "\" Unknown file type: " + file.Properties.Description);
			}

			try
			{
				string title = tag.Title;
				if (title != null)
				{
					song.SongName = title.Trim();
				}
			}
			catch
			{
				song.SongName = null;
			}

			try
			{
				song.TrackNumber = Convert.ToInt32(tag.Track);
			}
			catch
			{
				song.TrackNumber = null;
			}

			try
			{
				song.DiscNumber = Convert.ToInt32(tag.Disc);
			}
			catch
			{
				song.DiscNumber = null;
			}

			try
			{
				string firstGenre = tag.FirstGenre;
				if (firstGenre != null)
				{
					song.GenreName = firstGenre.Trim();
				}
			}
			catch
			{
				song.GenreName = null;
			}

			try
			{
				song.BeatsPerMinute = tag.BeatsPerMinute;
			}
			catch
			{
				song.BeatsPerMinute = null;
			}

			try
			{
				song.Lyrics = tag.Lyrics;
			}
			catch
			{
				song.Lyrics = null;
			}

			try
			{
				song.Comment = tag.Comment;
			}
			catch
			{
				song.Comment = null;
			}

			// Dispose tag
			tag = null;

			if ((object)song.GenreName != null)
			{
				// Retreive the genre id
				song.GenreId = Injection.Kernel.Get<IGenreRepository>().GenreForName(song.GenreName).GenreId;
			}

			song.Duration = Convert.ToInt32(file.Properties.Duration.TotalSeconds);
			song.Bitrate = file.Properties.AudioBitrate;

			// Get necessary filesystem information about file
			FileInfo fsFile = new FileInfo(filePath);

			song.FileSize = fsFile.Length;
			song.LastModified = fsFile.LastWriteTime.ToUniversalUnixTimestamp();
			song.FileName = fsFile.Name;

			// If there is no song name, use the file name
			if (song.SongName == null || song.SongName == "")
			{
				song.SongName = song.FileName;
			}

			// Generate an art id from the embedded art, if it exists
			int? artId = CreateArt(file).ArtId;

			// Dispose file handles
			fsFile = null;
			file.Dispose();

			// If there was no embedded art, use the folder's art
			artId = (object)artId == null ? Injection.Kernel.Get<IArtRepository>().ArtIdForItemId(song.FolderId) : artId;

			// Create the art/item relationship
			Injection.Kernel.Get<IArtRepository>().UpdateArtItemRelationship(artId, song.ItemId, true);

			return song;
		}

		private bool ArtFileNeedsUpdating(string filePath)
		{
			if (filePath == null)
			{
				return false;
			}

			long lastModified = System.IO.File.GetLastWriteTime(filePath).ToUniversalUnixTimestamp();
			bool needsUpdating = true;

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				string artId = conn.ExecuteScalar<string>("SELECT ArtId FROM Art WHERE LastModified = ? AND FilePath = ?", lastModified, filePath);

				if (!ReferenceEquals(artId, null))
				{
					needsUpdating = false;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return needsUpdating;
		}

		// used for getting art from a file.
		private Art CreateArt(string filePath)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

			// compute the hash of the file stream
			Art art = new Art();
			art.Md5Hash = fs.MD5();
			art.FileSize = fs.Length;
			art.LastModified = System.IO.File.GetLastWriteTime(fs.Name).ToUniversalUnixTimestamp();
			art.ArtId = Injection.Kernel.Get<IArtRepository>().ArtIdForMd5(art.Md5Hash);
			art.FilePath = filePath;

			if ((object)art.ArtId == null)
			{
				art.ArtId = Injection.Kernel.Get<IItemRepository>().GenerateItemId(ItemType.Art);
				Injection.Kernel.Get<IArtRepository>().InsertArt(art);
			}

			// Dispose file stream
			fs.Close();

			return art;
		}

		// used for getting art from a tag.
		// We don't set the FilePath here, because that is only used for actual art files on disk
		private Art CreateArt(TagLib.File file)
		{
			Art art = new Art();

			if (file.Tag.Pictures.Length > 0)
			{
				byte[] data = file.Tag.Pictures[0].Data.Data;
				art.Md5Hash = data.MD5();
				art.FileSize = data.Length;
				art.LastModified = System.IO.File.GetLastWriteTime(file.Name).ToUniversalUnixTimestamp();

				art.ArtId = Injection.Kernel.Get<IArtRepository>().ArtIdForMd5(art.Md5Hash);
				if (art.ArtId == null)
				{
					// This art isn't in the database yet, so add it
					art.ArtId = Injection.Kernel.Get<IItemRepository>().GenerateItemId(ItemType.Art);
					Injection.Kernel.Get<IArtRepository>().InsertArt(art);
				}
			}

			// Close file handle
			file.Dispose();

			return art;
		}

		private bool FileNeedsUpdating(string filePath, int? folderId, out bool isNew, out int? itemId)
		{
			ItemType type = Injection.Kernel.Get<IItemRepository>().ItemTypeForFilePath(filePath);

			bool needsUpdating = false;
			isNew = false;
			itemId = null;

			if (type == ItemType.Song)
			{
				needsUpdating = SongNeedsUpdating(filePath, folderId, out isNew, out itemId);
			}
			else if (type == ItemType.Video)
			{
				needsUpdating = VideoNeedsUpdating(filePath, folderId, out isNew, out itemId);
			}

			return needsUpdating;
		}

		private bool VideoNeedsUpdating(string filePath, int? folderId, out bool isNew, out int? itemId)
		{
			string fileName = Path.GetFileName(filePath);
			long lastModified = System.IO.File.GetLastWriteTime(filePath).ToUniversalUnixTimestamp();
			bool needsUpdating = true;
			isNew = true;
			itemId = null;

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				var result = conn.DeferredQuery<Video>("SELECT * FROM Video WHERE FolderId = ? AND FileName = ?", folderId, fileName);

				foreach (Video video in result)
				{
					isNew = false;

					itemId = video.ItemId;
					if (video.LastModified == lastModified)
					{
						needsUpdating = false;
					}

					break;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return needsUpdating;
		}

		public bool SongNeedsUpdating(string filePath, int? folderId, out bool isNew, out int? itemId)
		{
			// We don't need to instantiate another folder to know what the folder id is.  This should be known when the method is called.
			string fileName = Path.GetFileName(filePath);
			long lastModified = System.IO.File.GetLastWriteTime(filePath).ToUniversalUnixTimestamp();
			bool needsUpdating = true;
			isNew = true;
			itemId = null;

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				IEnumerable result = conn.Query<Song>("SELECT * FROM Song WHERE FolderId = ? AND FileName = ? LIMIT 1", folderId, fileName);

				foreach (Song song in result)
				{
					isNew = false;

					itemId = song.ItemId;
					if (song.LastModified == lastModified)
					{
						needsUpdating = false;
					}

					return needsUpdating;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return needsUpdating;
		}

		private Video CreateVideo(string filePath, int? folderId, TagLib.File file)
		{
			int? itemId = Injection.Kernel.Get<IItemRepository>().GenerateItemId(ItemType.Video);
			if (itemId == null)
			{
				return new Video();
			}

			Video video = new Video();
			video.ItemId = itemId;

			video.FolderId = folderId;
			video.FileType = video.FileType.FileTypeForTagLibMimeType(file.MimeType);

			if (video.FileType == FileType.Unknown)
			{
				logger.IfInfo("\"" + filePath + "\" Unknown file type: " + file.Properties.Description);
			}

			video.Width = file.Properties.VideoWidth;
			video.Height = file.Properties.VideoHeight;
			video.Duration = Convert.ToInt32(file.Properties.Duration.TotalSeconds);
			video.Bitrate = file.Properties.AudioBitrate;

			// Get filesystem information about file
			FileInfo fsFile = new FileInfo(filePath);

			video.FileSize = fsFile.Length;
			video.LastModified = fsFile.LastWriteTime.ToUniversalUnixTimestamp();
			video.FileName = fsFile.Name;

			// Generate an art id from the embedded art, if it exists
			int? artId = CreateArt(file).ArtId;

			// Close file handles
			fsFile = null;
			file.Dispose();

			// If there was no embedded art, use the folder's art
			artId = (object)artId == null ? Injection.Kernel.Get<IArtRepository>().ArtIdForItemId(video.FolderId) : artId;

			// Create the art/item relationship
			Injection.Kernel.Get<IArtRepository>().UpdateArtItemRelationship(artId, video.ItemId, true);

			return video;
		}
	}
}
