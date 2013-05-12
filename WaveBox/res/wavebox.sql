PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
CREATE TABLE item_type (
item_type_id INTEGER PRIMARY KEY ASC AUTOINCREMENT,
item_type TEXT UNIQUE
);
INSERT INTO "item_type" VALUES(2,'album');
INSERT INTO "item_type" VALUES(1,'artist');
INSERT INTO "item_type" VALUES(11,'bookmark');
INSERT INTO "item_type" VALUES(12,'bookmark_item');
INSERT INTO "item_type" VALUES(4,'folder');
INSERT INTO "item_type" VALUES(5,'playlist');
INSERT INTO "item_type" VALUES(6,'playlist_item');
INSERT INTO "item_type" VALUES(7,'podcast');
INSERT INTO "item_type" VALUES(8,'podcast_episode');
INSERT INTO "item_type" VALUES(3,'song');
INSERT INTO "item_type" VALUES(9,'user');
INSERT INTO "item_type" VALUES(10,'video');
CREATE TABLE "bookmark" (
"bookmark_id" INTEGER UNIQUE NOT NULL,
"bookmark_name" TEXT
);
CREATE TABLE "folder" (
"folder_id" INTEGER UNIQUE NOT NULL,
"folder_name" TEXT NOT NULL,
"folder_path" TEXT NOT NULL,
"parent_folder_id" INTEGER,
"folder_media_folder_id" INTEGER
);
CREATE TABLE "playlist" (
"playlist_id" INTEGER UNIQUE NOT NULL,
"playlist_name" TEXT,
"playlist_count" INTEGER,
"playlist_duration" INTEGER,
"md5_hash" TEXT,
"last_update" INTEGER
);
CREATE TABLE "podcast" (
"podcast_id" INTEGER UNIQUE NOT NULL,
"podcast_keep_cap" INTEGER,
"podcast_title" TEXT,
"podcast_author" TEXT,
"podcast_description" TEXT,
"podcast_rss_url" TEXT
);
CREATE TABLE "podcast_episode" (
"podcast_episode_id" INTEGER UNIQUE NOT NULL,
"podcast_episode_podcast_id" INTEGER,
"podcast_episode_title" TEXT,
"podcast_episode_author" TEXT,
"podcast_episode_subtitle" TEXT,
"podcast_episode_media_url" TEXT,
"podcast_episode_file_path" TEXT
);
CREATE TABLE "transcoding" (
"trans_id" INTEGER UNIQUE NOT NULL,
"trans_name" TEXT,
"from_file_type_id" INTEGER,
"to_file_type_id" INTEGER,
"trans_rule" TEXT
);
CREATE TABLE "genre" (
"genre_id" INTEGER UNIQUE NOT NULL,
"genre_name" TEXT NOT NULL
);
CREATE TABLE "bookmark_item" (
"bookmark_item_id" INTEGER UNIQUE NOT NULL,
"bookmark_id" INTEGER,
"item_type_id" INTEGER,
"item_id" INTEGER,
"item_position" INTEGER
);
CREATE TABLE "playlist_item" (
"playlist_item_id" INTEGER UNIQUE NOT NULL,
"playlist_id" INTEGER NOT NULL,
"item_type_id" INTEGER NOT NULL,
"item_id" INTEGER NOT NULL,
"item_position" INTEGER NOT NULL
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
CREATE TABLE "session" (
"session_id" TEXT UNIQUE NOT NULL,
"user_id" INTEGER NOT NULL,
"client_name" TEXT,
"create_time" INTEGER NOT NULL,
"update_time" INTEGER NOT NULL
);
CREATE TABLE "album" (
"album_id" INTEGER UNIQUE NOT NULL,
"album_name" TEXT NOT NULL,
"artist_id" INTEGER,
"album_release_year" INTEGER
);
CREATE TABLE "art_item" (
"art_id" INTEGER NOT NULL,
"item_id" INTEGER NOT NULL UNIQUE
);
CREATE TABLE "artist" (
"artist_id" integer UNIQUE NOT NULL,
"artist_name" text UNIQUE NOT NULL ON CONFLICT IGNORE
);
CREATE TABLE "art" (
"art_id" INTEGER NOT NULL UNIQUE,
"md5_hash" TEXT NOT NULL,
"art_last_modified" INTEGER,
"art_file_size" INTEGER,
"art_file_path" TEXT
);
CREATE TABLE "user" (
"user_id" INTEGER UNIQUE NOT NULL,
"user_name" TEXT UNIQUE NOT NULL,
"user_password" TEXT NOT NULL,
"user_salt" TEXT NOT NULL,
"user_lastfm_session" TEXT
);
CREATE TABLE "stat" (
"stat_id" INTEGER PRIMARY KEY NOT NULL,
"time_stamp" INTEGER NOT NULL,
"item_id" INTEGER NOT NULL,
"stat_type" INTEGER NOT NULL
);
CREATE TABLE "item" (
"item_id" INTEGER PRIMARY KEY NOT NULL,
"item_type_id" INTEGER NOT NULL,
"time_stamp" INTEGER NOT NULL
);
CREATE TABLE "server" (
"guid",
"url"
);
CREATE TABLE "song" (
    "song_id" INTEGER UNIQUE NOT NULL,
    "song_folder_id" INTEGER,
    "song_artist_id" INTEGER,
    "song_album_id" INTEGER,
    "song_file_type_id" INTEGER,
    "song_name" TEXT,
    "song_track_num" INTEGER,
    "song_disc_num" INTEGER,
    "song_duration" INTEGER,
    "song_bitrate" INTEGER,
    "song_file_size" INTEGER,
    "song_last_modified" INTEGER,
    "song_file_name" TEXT,
    "song_release_year" INTEGER, 
    "song_genre_id" INTEGER);
CREATE TABLE "video" (
    "video_id" INTEGER UNIQUE NOT NULL,
    "video_folder_id" INTEGER,
    "video_duration" INTEGER,
    "video_bitrate" INTEGER,
    "video_file_size" INTEGER,
    "video_last_modified" INTEGER,
    "video_file_name" TEXT,
    "video_width" INTEGER,
    "video_height" INTEGER,
    "video_file_type_id" INTEGER,
    "video_genre_id" INTEGER
);
DELETE FROM sqlite_sequence;
INSERT INTO "sqlite_sequence" VALUES('item_type',12);
CREATE INDEX "folder_parent" ON "folder" ("folder_name", "parent_folder_id");
CREATE INDEX "folder_path" ON "folder" ("folder_path");
CREATE UNIQUE INDEX "folderNamePath" ON "folder" ("folder_name","folder_path");
CREATE UNIQUE INDEX "title_author_unique" ON "podcast" ("podcast_title", "podcast_author");
CREATE UNIQUE INDEX "bookmarkItem" ON "bookmark_item" (bookmark_id, item_position);
CREATE UNIQUE INDEX "playlistPosition" ON "playlist_item" (playlist_id, item_position);
CREATE UNIQUE INDEX "albumNameArtistId" ON "album" ("album_name", "artist_id");
CREATE INDEX art_md5 ON art(md5_hash);
CREATE INDEX art_last_mod_path ON art("art_last_modified","art_file_path");
CREATE INDEX "stat_time" ON "stat" (time_stamp);
CREATE INDEX item_timestamp ON item (time_stamp);
CREATE INDEX "songId" ON "song" ("song_id");
CREATE INDEX "songNameFolderId" ON "song" ("song_folder_id","song_file_name");
COMMIT;
