
PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
CREATE TABLE ItemType (
ItemTypeId INTEGER PRIMARY KEY ASC AUTOINCREMENT,
ItemTypeName TEXT UNIQUE
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
"EpisodeKeepCap" INTEGER,
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
CREATE TABLE "Transcoding" (
"trans_id" INTEGER UNIQUE NOT NULL,
"trans_name" TEXT,
"from_file_type_id" INTEGER,
"to_file_type_id" INTEGER,
"trans_rule" TEXT
);
CREATE TABLE "BookmarkItem" (
"bookmark_item_id" INTEGER UNIQUE NOT NULL,
"bookmark_id" INTEGER,
"item_type_id" INTEGER,
"item_id" INTEGER,
"item_position" INTEGER
);
CREATE TABLE "file_type" (
"file_type_id" INTEGER UNIQUE NOT NULL,
"file_type_name" TEXT NOT NULL
);
INSERT INTO "file_type" VALUES(1,'AAC');
INSERT INTO "file_type" VALUES(2,'MP3');
INSERT INTO "file_type" VALUES(3,'MPC');
INSERT INTO "file_type" VALUES(4,'OGG');
INSERT INTO "file_type" VALUES(5,'WMA');
INSERT INTO "file_type" VALUES(6,'ALAC');
INSERT INTO "file_type" VALUES(7,'APE');
INSERT INTO "file_type" VALUES(8,'FLAC');
INSERT INTO "file_type" VALUES(9,'WV');
INSERT INTO "file_type" VALUES(2147483647,'Unknown');
CREATE TABLE "stat" (
"stat_id" INTEGER PRIMARY KEY NOT NULL,
"time_stamp" INTEGER NOT NULL,
"item_id" INTEGER NOT NULL,
"stat_type" INTEGER NOT NULL
);
CREATE TABLE "server" (
"guid",
"url"
);
CREATE TABLE "user" (
"user_id" INTEGER UNIQUE NOT NULL,
"user_name" TEXT UNIQUE NOT NULL,
"user_password" TEXT NOT NULL,
"user_salt" TEXT NOT NULL,
"user_lastfm_session" TEXT,
"create_time" INTEGER NOT NULL,
"delete_time" INTEGER
);
CREATE TABLE "folder" (
"FolderId" INTEGER UNIQUE NOT NULL,
"FolderName" TEXT NOT NULL,
"FolderPath" TEXT NOT NULL,
"ParentFolderId" INTEGER,
"MediaFolderId" INTEGER
);
CREATE TABLE "song" (
    "ItemId" INTEGER UNIQUE NOT NULL,
    "FolderId" INTEGER,
    "ArtistId" INTEGER,
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
    "GenreId" INTEGER);
CREATE TABLE "album" (
"ItemId" INTEGER UNIQUE NOT NULL,
"AlbumName" TEXT NOT NULL,
"ArtistId" INTEGER,
"ReleaseYear" INTEGER
);
CREATE TABLE "artist" (
"ItemId" integer UNIQUE NOT NULL,
"ArtistName" text UNIQUE NOT NULL ON CONFLICT IGNORE
);
CREATE TABLE "genre" (
"GenreId" INTEGER UNIQUE NOT NULL,
"GenreName" TEXT NOT NULL
);
CREATE TABLE "art" (
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
CREATE TABLE "video" (
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
CREATE TABLE "item" (
"ItemId" INTEGER PRIMARY KEY NOT NULL,
"ItemType" INTEGER NOT NULL,
"Timestamp" INTEGER NOT NULL
);
CREATE TABLE "playlist" (
"PlaylistId" INTEGER UNIQUE NOT NULL,
"PlaylistName" TEXT,
"PlaylistCount" INTEGER,
"PlaylistDuration" INTEGER,
"Md5Hash" TEXT,
"LastUpdateTime" INTEGER
);
CREATE TABLE "playlist_item" (
"PlaylistItemId" INTEGER UNIQUE NOT NULL,
"PlaylistId" INTEGER NOT NULL,
"ItemType" INTEGER NOT NULL,
"ItemId" INTEGER NOT NULL,
"ItemPosition" INTEGER NOT NULL
);
CREATE TABLE "session" (
"SessionId" TEXT UNIQUE NOT NULL,
"UserId" INTEGER NOT NULL,
"ClientName" TEXT,
"CreateTime" INTEGER NOT NULL,
"UpdateTime" INTEGER NOT NULL
);
DELETE FROM sqlite_sequence;
INSERT INTO "sqlite_sequence" VALUES('item_type',12);
CREATE UNIQUE INDEX "title_author_unique" ON "podcast" ("podcast_title", "podcast_author");
CREATE UNIQUE INDEX "bookmarkItem" ON "bookmark_item" (bookmark_id, item_position);
CREATE INDEX "stat_time" ON "stat" (time_stamp);
CREATE UNIQUE INDEX "folder_NamePath" ON "folder" ("FolderName","FolderPath");
CREATE INDEX "folder_Parent" ON "folder" ("FolderName", "ParentFolderId");
CREATE INDEX "folder_Path" ON "folder" ("FolderPath");
CREATE INDEX "song_ItemId" ON "song" ("ItemId");
CREATE INDEX "song_FolderIdFileName" ON "song" ("FolderId","FileName");
CREATE UNIQUE INDEX "album_AlbumNameArtistId" ON "album" ("AlbumName", "ArtistId");
CREATE INDEX art_LastModFilePath ON art("LastModified","FilePath");
CREATE INDEX art_Md5Hash ON art("Md5Hash");
CREATE INDEX item_Timestamp ON item (Timestamp);
CREATE UNIQUE INDEX "playlist_item_PlaylistIdPosition" ON "playlist_item" (PlaylistId, ItemPosition);
COMMIT;
