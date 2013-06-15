using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Static;
using WaveBox.Model;
using System.Data;
using System.IO;
using System.Diagnostics;
using WaveBox.OperationQueue;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.FolderScanning
{
	public class OrphanScanOperation : AbstractOperation
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override string OperationType { get { return "OrphanScanOperation"; } }

		long totalExistsTime = 0;

		public OrphanScanOperation(int delayMilliSeconds) : base(delayMilliSeconds)
		{
		}

		public override void Start()
		{
			Stopwatch sw = new Stopwatch();

			if (logger.IsInfoEnabled) logger.Info("---------------- ORPHAN SCAN ----------------");
			sw.Start();
			CheckFolders();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check folders: " + sw.ElapsedMilliseconds + "ms");

			sw.Restart();
			CheckSongs();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check songs: " + sw.ElapsedMilliseconds + "ms");

			sw.Restart();
			CheckArtists();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check artists: " + sw.ElapsedMilliseconds + "ms");

			sw.Restart();
			CheckAlbums();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check albums: " + sw.ElapsedMilliseconds + "ms");

			sw.Restart();
			CheckGenres();
			sw.Stop();
			if (logger.IsInfoEnabled) logger.Info("check genres: " + sw.ElapsedMilliseconds + "ms");

			if (logger.IsInfoEnabled) logger.Info("check songs exists calls total time: " + (totalExistsTime / 10000) + "ms"); // Convert ticks to milliseconds, divide by 10,000
			if (logger.IsInfoEnabled) logger.Info("---------------------------------------------");
		}

		private void CheckFolders()
		{
			if (isRestart) 
			{
				return;
			}

			ArrayList mediaFolderIds = new ArrayList();
			ArrayList orphanFolderIds = new ArrayList();

			foreach (Folder mediaFolder in Settings.MediaFolders) {
				mediaFolderIds.Add (mediaFolder.FolderId);
			}

			ISQLiteConnection conn = null;
			try 
			{
				conn = Database.GetSqliteConnection();

				// Find the orphaned folders
				var result = conn.DeferredQuery<Folder>("SELECT * FROM Folder");
				foreach (Folder folder in result)
				{
					if (folder.MediaFolderId != null)
					{
						if (!mediaFolderIds.Contains(folder.MediaFolderId) || !Directory.Exists(folder.FolderPath)) 
						{
							logger.Info(folder.FolderId + " is orphaned");
							orphanFolderIds.Add(folder.FolderId);
						}
					}
				}

				// Remove them
				foreach (int folderId in orphanFolderIds) 
				{
					try 
					{
						conn.ExecuteLogged("DELETE FROM Folder WHERE FolderId = ?", folderId);
					} 
					catch (Exception e) 
					{
						logger.Error("Failed to delete orphan " + folderId + " : " + e);
					}

					try
					{
						conn.ExecuteLogged("DELETE FROM Song WHERE FolderId = ?", folderId);
					} 
					catch (Exception e) 
					{
						logger.Error("Failed to delete songs for orphan " + folderId + " : " + e);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed to delete orphan items : " + e);
			}
			finally
			{
				conn.Close();
			}
		}

		private void CheckSongs()
		{
			if (isRestart)
			{
				return;
			}

			ArrayList orphanSongIds = new ArrayList();

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();

				// Find the orphaned songs
				var result = conn.DeferredQuery<Song>("SELECT * FROM Song");
				foreach (Song song in result)
				{
					long timestamp = DateTime.Now.ToUniversalUnixTimestamp();
					bool exists = File.Exists(song.FilePath);
					totalExistsTime += DateTime.Now.ToUniversalUnixTimestamp() - timestamp;

					if (!exists)
					{
						orphanSongIds.Add(song.ItemId);
					}
				}

				// Remove them
				foreach (int itemId in orphanSongIds)
				{
					try
					{
						conn.ExecuteLogged("DELETE FROM Song WHERE ItemId = ?", itemId);
						if (logger.IsInfoEnabled) logger.Info("Song " + itemId + " deleted");
					}
					catch (Exception e)
					{
						logger.Error("Failed deleting orphan songs : " + e);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed checking for orphan songs " + e);
			}
			finally
			{
				conn.Close();
			}
		}

		private void CheckArtists()
		{
			if (isRestart)
			{
				return;
			}

			ArrayList orphanArtistIds = new ArrayList();

			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();

				// Find the orphaned artists
				var result = conn.DeferredQuery<Artist>("SELECT Artist.ArtistId FROM Artist " +
				                                        "LEFT JOIN Song ON Artist.ArtistId = Song.ArtistId " +
				                                        "WHERE Song.ArtistId IS NULL"); 
				foreach (Artist artist in result)
				{
					orphanArtistIds.Add(artist.ArtistId);
				}

				// Remove them
				foreach (int artistId in orphanArtistIds)
				{
					try
					{
						conn.ExecuteLogged("DELETE FROM Artist WHERE ArtistId = ?", artistId);
						if (logger.IsInfoEnabled) logger.Info("Artist " + artistId + " deleted");
					}
					catch (Exception e)
					{
						logger.Error("Failed deleting orphan artists" + e);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed checking for orphan artists : " + e);
			}
			finally
			{
				conn.Close();
			}
		}

		private void CheckAlbums()
		{
			if (isRestart)
			{
				return;
			}

			ArrayList orphanAlbumIds = new ArrayList();

			ISQLiteConnection conn = null;
			try
			{
				// Find the orphaned albums
				conn = Database.GetSqliteConnection();
				var result = conn.DeferredQuery<Album>("SELECT Album.AlbumId FROM Album " +
				                                       "LEFT JOIN Song ON Album.AlbumId = Song.AlbumId " + 
				                                       "WHERE Song.AlbumId IS NULL");
				foreach (Album album in result)
				{
					orphanAlbumIds.Add(album.AlbumId);
				}

				// Remove them
				foreach (int albumId in orphanAlbumIds)
				{
					try
					{
						conn.ExecuteLogged("DELETE FROM Album WHERE AlbumId = ?", albumId);
						if (logger.IsInfoEnabled) logger.Info("Album " + albumId + " deleted");
					}
					catch (Exception e)
					{
						logger.Error("Failed deleting orphan albums " + e);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed checking for orphan albums" + e);
			}
			finally
			{
				conn.Close();
			}
		}

		private void CheckGenres()
		{
			if (isRestart)
			{
				return;
			}

			ArrayList orphanGenreIds = new ArrayList();

			ISQLiteConnection conn = null;
			try
			{
				// Find orphaned genres
				conn = Database.GetSqliteConnection();
				var result = conn.DeferredQuery<Genre>("SELECT Genre.GenreId FROM Genre " +
				                                       "LEFT JOIN Song ON Genre.GenreId = Song.GenreId " + 
				                                       "WHERE Song.GenreId IS NULL");
				foreach (Genre genre in result)
				{
					orphanGenreIds.Add(genre.GenreId);
				}

				// Remove them
				foreach (int genreId in orphanGenreIds)
				{
					try
					{
						conn.ExecuteLogged("DELETE FROM Genre WHERE GenreId = ?", genreId);
						if (logger.IsInfoEnabled) logger.Info("Genre " + genreId + " deleted");
					}
					catch (Exception e)
					{
						logger.Error("Failed deleting orphan genre " + genreId + ": " + e);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("Failed checking for orphan genres: " + e);
			}
			finally
			{
				conn.Close();
			}
		}
	}
}
