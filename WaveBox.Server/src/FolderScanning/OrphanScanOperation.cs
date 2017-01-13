using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.OperationQueue;
using WaveBox.Server.Extensions;
using WaveBox.Static;
using WaveBox.Core;
using WaveBox.Core.Model.Repository;

namespace WaveBox.FolderScanning {
    public class OrphanScanOperation : AbstractOperation {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public override string OperationType { get { return "OrphanScanOperation"; } }

        long totalExistsTime = 0;

        public OrphanScanOperation(int delayMilliSeconds) : base(delayMilliSeconds) {
        }

        public override void Start() {
            Stopwatch sw = new Stopwatch();

            logger.IfInfo("---------------- ORPHAN SCAN ----------------");
            logger.IfInfo("Folders:");
            sw.Start();
            this.CheckFolders();
            sw.Stop();
            logger.IfInfo("Done, elapsed: " + sw.ElapsedMilliseconds + "ms");

            logger.IfInfo("Songs:");
            sw.Restart();
            this.CheckSongs();
            sw.Stop();
            logger.IfInfo("Done, elapsed: " + sw.ElapsedMilliseconds + "ms");

            logger.IfInfo("Artists:");
            sw.Restart();
            this.CheckArtists();
            sw.Stop();
            logger.IfInfo("Done, elapsed: " + sw.ElapsedMilliseconds + "ms");

            logger.IfInfo("AlbumArtists:");
            sw.Restart();
            this.CheckAlbumArtists();
            sw.Stop();
            logger.IfInfo("Done, elapsed: " + sw.ElapsedMilliseconds + "ms");

            logger.IfInfo("Albums:");
            sw.Restart();
            this.CheckAlbums();
            sw.Stop();
            logger.IfInfo("Done, elapsed: " + sw.ElapsedMilliseconds + "ms");

            logger.IfInfo("Genres:");
            sw.Restart();
            this.CheckGenres();
            sw.Stop();
            logger.IfInfo("Done, elapsed: " + sw.ElapsedMilliseconds + "ms");

            logger.IfInfo("Videos:");
            sw.Restart();
            this.CheckVideos();
            sw.Stop();
            logger.IfInfo("Done, elapsed: " + sw.ElapsedMilliseconds + "ms");

            logger.IfInfo("DONE, TOTAL ELAPSED: " + (totalExistsTime / 10000) + "ms");
            logger.IfInfo("---------------------------------------------");
        }

        private void CheckFolders() {
            if (isRestart) {
                return;
            }

            ArrayList mediaFolderIds = new ArrayList();
            ArrayList orphanFolderIds = new ArrayList();

            foreach (Folder mediaFolder in Injection.Kernel.Get<IFolderRepository>().MediaFolders()) {
                mediaFolderIds.Add (mediaFolder.FolderId);
            }

            ISQLiteConnection conn = null;
            try {
                conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

                // Find the orphaned folders
                var result = conn.DeferredQuery<Folder>("SELECT * FROM Folder");
                foreach (Folder folder in result) {
                    if (folder.MediaFolderId != null) {
                        if (!mediaFolderIds.Contains(folder.MediaFolderId) || !Directory.Exists(folder.FolderPath)) {
                            logger.IfInfo(folder.FolderId + " is orphaned");
                            orphanFolderIds.Add(folder.FolderId);
                        }
                    }
                    // Alternatively, if folder was or is a root media folder, it won't have a media folder ID.
                    else {
                        // Check if it's in the list of root media folders.  If not, it's an orphan
                        bool success = false;
                        foreach (Folder f in Injection.Kernel.Get<IFolderRepository>().MediaFolders()) {
                            if (f.FolderPath == folder.FolderPath) {
                                success = true;
                                break;
                            }
                        }

                        // Add any orphan folders to purge list
                        if (!success) {
                            orphanFolderIds.Add(folder.FolderId);
                        }
                    }
                }

                // Remove them
                foreach (int folderId in orphanFolderIds) {
                    try {
                        conn.ExecuteLogged("DELETE FROM Folder WHERE FolderId = ?", folderId);
                        logger.IfInfo("  - Folder " + folderId + " deleted");
                    } catch (Exception e) {
                        logger.Error("Failed to delete orphan " + folderId + " : " + e);
                    }

                    try {
                        conn.ExecuteLogged("DELETE FROM Song WHERE FolderId = ?", folderId);
                    } catch (Exception e) {
                        logger.Error("Failed to delete songs for orphan " + folderId + " : " + e);
                    }
                }
            } catch (Exception e) {
                logger.Error("Failed to delete orphan items : " + e);
            } finally {
                Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
            }
        }

        private void CheckSongs() {
            if (isRestart) {
                return;
            }

            ArrayList orphanSongIds = new ArrayList();

            ISQLiteConnection conn = null;
            try {
                conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

                // Find the orphaned songs
                var result = conn.DeferredQuery<Song>("SELECT * FROM Song");
                foreach (Song song in result) {
                    long timestamp = DateTime.UtcNow.ToUnixTime();
                    bool exists = File.Exists(song.FilePath());
                    totalExistsTime += DateTime.UtcNow.ToUnixTime() - timestamp;

                    if (!exists) {
                        orphanSongIds.Add(song.ItemId);
                    }
                }

                // Remove them
                foreach (int itemId in orphanSongIds) {
                    try {
                        conn.ExecuteLogged("DELETE FROM Song WHERE ItemId = ?", itemId);
                        logger.IfInfo("Song " + itemId + " deleted");
                    } catch (Exception e) {
                        logger.Error("Failed deleting orphan songs : " + e);
                    }
                }
            } catch (Exception e) {
                logger.Error("Failed checking for orphan songs " + e);
            } finally {
                Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
            }
        }

        private void CheckArtists() {
            if (isRestart) {
                return;
            }

            ArrayList orphanArtistIds = new ArrayList();

            ISQLiteConnection conn = null;
            try {
                conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

                // Find the orphaned artists
                var result = conn.DeferredQuery<Artist>("SELECT Artist.ArtistId FROM Artist " +
                                                        "LEFT JOIN Song ON Artist.ArtistId = Song.ArtistId " +
                                                        "WHERE Song.ArtistId IS NULL");
                foreach (Artist artist in result) {
                    orphanArtistIds.Add(artist.ArtistId);
                }

                // Remove them
                foreach (int artistId in orphanArtistIds) {
                    try {
                        conn.ExecuteLogged("DELETE FROM Artist WHERE ArtistId = ?", artistId);
                        logger.IfInfo("Artist " + artistId + " deleted");
                    } catch (Exception e) {
                        logger.Error("Failed deleting orphan artists" + e);
                    }
                }
            } catch (Exception e) {
                logger.Error("Failed checking for orphan artists : " + e);
            } finally {
                Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
            }
        }

        private void CheckAlbumArtists() {
            if (isRestart) {
                return;
            }

            ArrayList orphanAlbumArtistIds = new ArrayList();

            ISQLiteConnection conn = null;
            try {
                conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();

                // Find the orphaned album artists
                var result = conn.DeferredQuery<AlbumArtist>("SELECT AlbumArtist.AlbumArtistId FROM AlbumArtist " +
                             "LEFT JOIN Song ON AlbumArtist.AlbumArtistId = Song.AlbumArtistId " +
                             "WHERE Song.AlbumArtistId IS NULL");
                foreach (AlbumArtist albumArtist in result) {
                    orphanAlbumArtistIds.Add(albumArtist.AlbumArtistId);
                }

                // Remove them
                foreach (int albumArtistId in orphanAlbumArtistIds) {
                    try {
                        conn.ExecuteLogged("DELETE FROM AlbumArtist WHERE AlbumArtistId = ?", albumArtistId);
                        logger.IfInfo("AlbumArtist " + albumArtistId + " deleted");
                    } catch (Exception e) {
                        logger.Error("Failed deleting orphan album artists" + e);
                    }
                }
            } catch (Exception e) {
                logger.Error("Failed checking for orphan album artists : " + e);
            } finally {
                Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
            }
        }

        private void CheckAlbums() {
            if (isRestart) {
                return;
            }

            ArrayList orphanAlbumIds = new ArrayList();

            ISQLiteConnection conn = null;
            try {
                // Find the orphaned albums
                conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
                var result = conn.DeferredQuery<Album>("SELECT Album.AlbumId FROM Album " +
                                                       "LEFT JOIN Song ON Album.AlbumId = Song.AlbumId " +
                                                       "WHERE Song.AlbumId IS NULL");
                foreach (Album album in result) {
                    orphanAlbumIds.Add(album.AlbumId);
                }

                // Remove them
                foreach (int albumId in orphanAlbumIds) {
                    try {
                        conn.ExecuteLogged("DELETE FROM Album WHERE AlbumId = ?", albumId);
                        logger.IfInfo("Album " + albumId + " deleted");
                    } catch (Exception e) {
                        logger.Error("Failed deleting orphan albums " + e);
                    }
                }
            } catch (Exception e) {
                logger.Error("Failed checking for orphan albums" + e);
            } finally {
                Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
            }
        }

        private void CheckGenres() {
            if (isRestart) {
                return;
            }

            ArrayList orphanGenreIds = new ArrayList();

            ISQLiteConnection conn = null;
            try {
                // Find orphaned genres
                conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
                var result = conn.DeferredQuery<Genre>("SELECT Genre.GenreId FROM Genre " +
                                                       "LEFT JOIN Song ON Genre.GenreId = Song.GenreId " +
                                                       "WHERE Song.GenreId IS NULL");
                foreach (Genre genre in result) {
                    orphanGenreIds.Add(genre.GenreId);
                }

                // Remove them
                foreach (int genreId in orphanGenreIds) {
                    try {
                        conn.ExecuteLogged("DELETE FROM Genre WHERE GenreId = ?", genreId);
                        logger.IfInfo("Genre " + genreId + " deleted");
                    } catch (Exception e) {
                        logger.Error("Failed deleting orphan genre " + genreId + ": " + e);
                    }
                }
            } catch (Exception e) {
                logger.Error("Failed checking for orphan genres: " + e);
            } finally {
                Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
            }
        }

        private void CheckVideos() {
            if (isRestart) {
                return;
            }

            ArrayList orphanVideoIds = new ArrayList();

            ISQLiteConnection conn = null;
            try {
                // Check for videos which don't have a folder path, meaning that they're orphaned
                conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
                var result = conn.DeferredQuery<Video>("SELECT Video.ItemId FROM Video " +
                                                       "LEFT JOIN Folder ON Video.FolderId = Folder.FolderId " +
                                                       "WHERE Folder.FolderPath IS NULL");

                foreach (Video video in result) {
                    orphanVideoIds.Add(video.ItemId);
                }

                // Remove them
                foreach (int itemId in orphanVideoIds) {
                    try {
                        conn.ExecuteLogged("DELETE FROM Video WHERE ItemId = ?", itemId);
                        logger.IfInfo("  - Video " + itemId + " deleted");
                    } catch (Exception e) {
                        logger.Error("Failed deleting orphan videos : " + e);
                    }
                }
            } catch (Exception e) {
                logger.Error("Failed checking for orphan videos " + e);
            } finally {
                Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
            }
        }
    }
}
