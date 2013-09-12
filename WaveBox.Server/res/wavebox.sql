
PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
CREATE TABLE ItemType (
    "ItemTypeId" INTEGER PRIMARY KEY ASC AUTOINCREMENT,
    "Name" TEXT UNIQUE
);
INSERT INTO "ItemType" VALUES(1,'artist');
INSERT INTO "ItemType" VALUES(2,'album');
INSERT INTO "ItemType" VALUES(3,'song');
INSERT INTO "ItemType" VALUES(4,'folder');
INSERT INTO "ItemType" VALUES(5,'playlist');
INSERT INTO "ItemType" VALUES(6,'playlist_item');
INSERT INTO "ItemType" VALUES(7,'podcast');
INSERT INTO "ItemType" VALUES(8,'podcast_episode');
INSERT INTO "ItemType" VALUES(9,'user');
INSERT INTO "ItemType" VALUES(10,'video');
INSERT INTO "ItemType" VALUES(11,'bookmark');
INSERT INTO "ItemType" VALUES(12,'bookmark_item');
CREATE TABLE "Bookmark" (
    "BookmarkId" INTEGER UNIQUE NOT NULL,
    "BookmarkName" TEXT
);
CREATE TABLE "Podcast" (
    "PodcastId" INTEGER UNIQUE NOT NULL,
    "KeepCap" INTEGER,
    "Title" TEXT,
    "Author" TEXT,
    "Description" TEXT,
    "RssUrl" TEXT
);
CREATE TABLE "PodcastEpisode" (
    "EpisodeId" INTEGER UNIQUE NOT NULL,
    "PodcastId" INTEGER,
    "Title" TEXT,
    "Author" TEXT,
    "Subtitle" TEXT,
    "MediaUrl" TEXT,
    "FilePath" TEXT
);
CREATE TABLE "BookmarkItem" (
    "BookmarkItemId" INTEGER UNIQUE NOT NULL,
    "BookmarkId" INTEGER,
    "ItemType" INTEGER,
    "ItemId" INTEGER,
    "ItemPosition" INTEGER
);
CREATE TABLE "FileType" (
    "FileTypeId" INTEGER UNIQUE NOT NULL,
    "Name" TEXT NOT NULL
);
INSERT INTO "FileType" VALUES(1,'AAC');
INSERT INTO "FileType" VALUES(2,'MP3');
INSERT INTO "FileType" VALUES(3,'MPC');
INSERT INTO "FileType" VALUES(4,'OGG');
INSERT INTO "FileType" VALUES(5,'WMA');
INSERT INTO "FileType" VALUES(6,'ALAC');
INSERT INTO "FileType" VALUES(7,'APE');
INSERT INTO "FileType" VALUES(8,'FLAC');
INSERT INTO "FileType" VALUES(9,'WV');
INSERT INTO "FileType" VALUES(2147483647,'Unknown');
CREATE TABLE "Server" (
    "Guid",
    "Url"
);
CREATE TABLE "Folder" (
    "FolderId" INTEGER UNIQUE NOT NULL,
    "FolderName" TEXT NOT NULL,
    "FolderPath" TEXT NOT NULL,
    "ParentFolderId" INTEGER,
    "MediaFolderId" INTEGER
);
CREATE TABLE "Genre" (
    "GenreId" INTEGER UNIQUE NOT NULL,
    "GenreName" TEXT NOT NULL
);
CREATE TABLE "Art" (
    "ArtId" INTEGER NOT NULL UNIQUE,
    "Md5Hash" TEXT NOT NULL,
    "LastModified" INTEGER,
    "FileSize" INTEGER,
    "FilePath" TEXT
);
CREATE TABLE "ArtItem" (
    "ArtId" INTEGER NOT NULL,
    "ItemId" INTEGER NOT NULL UNIQUE
);
CREATE TABLE "Video" (
    "ItemId" INTEGER UNIQUE NOT NULL,
    "FolderId" INTEGER,
    "Duration" INTEGER,
    "Bitrate" INTEGER,
    "FileSize" INTEGER,
    "LastModified" INTEGER,
    "FileName" TEXT,
    "Width" INTEGER,
    "Height" INTEGER,
    "FileType" INTEGER,
    "GenreId" INTEGER
);
CREATE TABLE "Item" (
    "ItemId" INTEGER PRIMARY KEY NOT NULL,
    "ItemType" INTEGER NOT NULL,
    "Timestamp" INTEGER NOT NULL
);
CREATE TABLE "Playlist" (
    "PlaylistId" INTEGER UNIQUE NOT NULL,
    "PlaylistName" TEXT,
    "PlaylistCount" INTEGER,
    "PlaylistDuration" INTEGER,
    "Md5Hash" TEXT,
    "LastUpdateTime" INTEGER
);
CREATE TABLE "PlaylistItem" (
    "PlaylistItemId" INTEGER UNIQUE NOT NULL,
    "PlaylistId" INTEGER NOT NULL,
    "ItemType" INTEGER NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "ItemPosition" INTEGER NOT NULL
);
CREATE TABLE "Session" (
    "SessionId" TEXT UNIQUE NOT NULL,
    "UserId" INTEGER NOT NULL,
    "ClientName" TEXT,
    "CreateTime" INTEGER NOT NULL,
    "UpdateTime" INTEGER NOT NULL
);
CREATE TABLE "Stat" (
    "StatId" INTEGER PRIMARY KEY NOT NULL,
    "Timestamp" INTEGER NOT NULL,
    "ItemId" INTEGER NOT NULL,
    "StatType" INTEGER NOT NULL
);
CREATE TABLE "User" (
    "UserId" INTEGER UNIQUE NOT NULL,
    "UserName" TEXT UNIQUE NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "PasswordSalt" TEXT NOT NULL,
    "LastfmSession" TEXT,
    "CreateTime" INTEGER NOT NULL,
    "DeleteTime" INTEGER
);
CREATE TABLE "Song" (
    "ItemId" INTEGER UNIQUE NOT NULL,
    "FolderId" INTEGER,
    "ArtistId" INTEGER,
    "AlbumArtistId" INTEGER,
    "AlbumId" INTEGER,
    "FileType" INTEGER,
    "SongName" TEXT,
    "TrackNumber" INTEGER,
    "DiscNumber" INTEGER,
    "Duration" INTEGER,
    "Bitrate" INTEGER,
    "FileSize" INTEGER,
    "LastModified" INTEGER,
    "FileName" TEXT,
    "ReleaseYear" INTEGER,
    "GenreId" INTEGER,
    "BeatsPerMinute" INTEGER,
    "Lyrics" TEXT,
    "Comment" TEXT);
CREATE TABLE "Version" (
    "VersionNumber" INTEGER NOT NULL
);
CREATE TABLE "Favorite" (
    "FavoriteId" INTEGER NOT NULL UNIQUE,
    "FavoriteUserId" INTEGER NOT NULL,
    "FavoriteItemId" INTEGER NOT NULL,
    "FavoriteItemTypeId" INTEGER NOT NULL,
    "Timestamp" INTEGER NOT NULL
);
CREATE TABLE "Artist" (
    "ArtistId" integer UNIQUE NOT NULL,
    "ArtistName" text UNIQUE NOT NULL ON CONFLICT IGNORE,
    "MusicBrainzId" text
);
CREATE TABLE "AlbumArtist" (
    "AlbumArtistId" integer UNIQUE NOT NULL,
    "AlbumArtistName" text UNIQUE NOT NULL ON CONFLICT IGNORE,
    "MusicBrainzId" text
);
CREATE TABLE "Album" (
    "AlbumId" INTEGER UNIQUE NOT NULL,
    "AlbumName" TEXT NOT NULL,
    "AlbumArtistId" INTEGER,
    "ReleaseYear" INTEGER,
    "MusicBrainzId" text
);
CREATE TABLE "MusicBrainzCheckDate" (
    "ItemId" integer UNIQUE NOT NULL,
    "Timestamp" INTEGER NOT NULL
);
DELETE FROM sqlite_sequence;
INSERT INTO "sqlite_sequence" VALUES('item_type',12);
CREATE UNIQUE INDEX "title_author_unique" ON "Podcast" ("Title", "Author");
CREATE UNIQUE INDEX "bookmark_item" ON "BookmarkItem" ("BookmarkId", "ItemPosition");
CREATE UNIQUE INDEX "folder_NamePath" ON "Folder" ("FolderName","FolderPath");
CREATE INDEX "folder_Parent" ON "Folder" ("FolderName", "ParentFolderId");
CREATE INDEX "folder_Path" ON "Folder" ("FolderPath");
CREATE INDEX art_LastModFilePath ON art("LastModified","FilePath");
CREATE INDEX art_Md5Hash ON art("Md5Hash");
CREATE INDEX item_Timestamp ON item ("Timestamp");
CREATE UNIQUE INDEX "playlist_item_PlaylistIdPosition" ON "PlaylistItem" ("PlaylistId", "ItemPosition");
CREATE INDEX "stat_Timestamp" ON "Stat" ("Timestamp");
CREATE INDEX "song_FolderIdFileName" ON "song" ("FolderId","FileName");
CREATE INDEX "song_ItemId" ON "Song" ("ItemId");
CREATE INDEX "favorite_userId" ON "Favorite" ("FavoriteUserId");
CREATE UNIQUE INDEX "album_AlbumNameArtistId" ON "Album" ("AlbumName", "AlbumArtistId");
COMMIT;
