using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using System.IO;
using TagLib;
using Newtonsoft.Json;
using System.Diagnostics;

namespace WaveBox.DataModel.Model
{
	public class Song : MediaItem
	{
		[JsonProperty("itemTypeId")]
		public override int ItemTypeId
		{
			get
			{
				return (int)ItemType.SONG;
			}
		}

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("artistName")]
		public string ArtistName { get; set; }

		[JsonProperty("albumId")]
		public int? AlbumId { get; set; }

		[JsonProperty("albumName")]
		public string AlbumName { get; set; }

		[JsonProperty("songName")]
		public string SongName { get; set; }

		[JsonProperty("trackNumber")]
		public int? TrackNumber { get; set; }

		[JsonProperty("discNumber")]
		public int? DiscNumber { get; set; }

		[JsonProperty("releaseYear")]
		public int? ReleaseYear { get; set; }

		public Song()
		{
		}

		public Song(int songId)
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " +
					"LEFT JOIN artist ON song_artist_id = artist.artist_id " +
					"LEFT JOIN album ON song_album_id = album.album_id " +
					"WHERE song_id = @songid", conn);
				q.AddNamedParam("@songid", songId);

				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					SetPropertiesFromQueryResult(reader);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[SONG(1)] " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public Song(System.IO.FileInfo fsFile, int? folderId, TagLib.File file)
		{
            // We need to check to make sure the tag isn't corrupt before handing off to this method, anyway, so just feed in the tag
            // file that we checked for corruption.
            //var file = TagLib.File.Create(fsFile.FullName);

			var tag = file.Tag;
            //var lol = file.Properties.Codecs;
			FolderId = folderId;

			try
			{
				var artist = Artist.ArtistForName(tag.FirstPerformer);
                ArtistId = artist.ArtistId;
                ArtistName = artist.ArtistName;
			}
			catch
			{
                ArtistId = null;
                ArtistName = null;
			}

            try
            {
                var album = Album.AlbumForName(tag.Album, ArtistId);
                AlbumId = album.AlbumId;
                AlbumName = album.AlbumName;
            }
            catch
            {
                AlbumId = null;
                AlbumName = null;
            }

			FileType = FileType.FileTypeForTagSharpString(file.Properties.Description);

			if (FileType == FileType.UNKNOWN)
				Console.WriteLine("[SONG] " + "Unknown file type: " + file.Properties.Description);

            try
            {
                SongName = tag.Title;
            }
            catch
            {
                SongName = null;
            }

            try
            {
                TrackNumber = Convert.ToInt32(tag.Track);
            }
            catch
            {
                TrackNumber = null;
            }

            try
            {
                DiscNumber = Convert.ToInt32(tag.Disc);
            }
            catch
            {
                DiscNumber = null;
            }

			try
			{
				ReleaseYear = Convert.ToInt32(tag.Year);
			}
			catch
			{
				ReleaseYear = null;
			}

            Duration = Convert.ToInt32(file.Properties.Duration.TotalSeconds);
            Bitrate = file.Properties.AudioBitrate;
            FileSize = fsFile.Length;
            LastModified = Convert.ToInt64(fsFile.LastWriteTime.Ticks);
            FileName = fsFile.Name;

			// check to see if the folder has art associated with it.  if it does, use that art.  if not,
			// check to see if the tag contains art.

			var folder = new Folder(folderId);

			if (folder.ContainsImageFile == false)
			{
				var art = new CoverArt(fsFile);
				ArtId = art.ArtId;
			}

			else ArtId = folder.ArtId;


		}

		public Song(IDataReader reader)
		{
			SetPropertiesFromQueryResult(reader);
		}

		private void SetPropertiesFromQueryResult(IDataReader reader)
		{
			try
			{
				ItemId = reader.GetInt32(reader.GetOrdinal("song_id"));
				FolderId = reader.GetInt32(reader.GetOrdinal("song_folder_id"));
				ArtistId = reader.GetInt32(reader.GetOrdinal("song_artist_id"));

				if 
					(reader.GetValue(reader.GetOrdinal("artist_name")) == DBNull.Value) ArtistName = "";
				else 
					ArtistName = reader.GetString(reader.GetOrdinal("artist_name"));

				AlbumId = reader.GetInt32(reader.GetOrdinal("song_album_id"));

				if 
					(reader.GetValue(reader.GetOrdinal("album_name")) == DBNull.Value) AlbumName = "";
				else 
					AlbumName = reader.GetString(reader.GetOrdinal("album_name"));

				FileType = FileType.FileTypeForId(reader.GetInt32(reader.GetOrdinal("song_file_type_id")));

				if (reader.GetValue(reader.GetOrdinal("song_name")) == DBNull.Value) SongName = "";
				else SongName = reader.GetString(reader.GetOrdinal("song_name"));

				TrackNumber = reader.GetInt32(reader.GetOrdinal("song_track_num"));
				DiscNumber = reader.GetInt32(reader.GetOrdinal("song_disc_num"));
				Duration = reader.GetInt32(reader.GetOrdinal("song_duration"));
				Bitrate = reader.GetInt32(reader.GetOrdinal("song_bitrate"));
				FileSize = reader.GetInt64(reader.GetOrdinal("song_file_size"));
				LastModified = reader.GetInt64(reader.GetOrdinal("song_last_modified"));
				FileName = reader.GetString(reader.GetOrdinal("song_file_name"));
				ReleaseYear = reader.GetInt32(reader.GetOrdinal("song_release_year"));

				if (reader.GetValue(reader.GetOrdinal("song_art_id")) == DBNull.Value) ArtId = null;
				else 
					ArtId = reader.GetInt32(reader.GetOrdinal("song_art_id"));
				//_artId = 0;
			}
			catch (Exception e)
			{
				Console.WriteLine("[SONG(2)] " + e.ToString());
			}
		}

		public void updateDatabase()
		{
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				// insert the song into the database
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO song (song_folder_id, song_artist_id, song_album_id, song_file_type_id, song_name, song_track_num, song_disc_num, song_duration, song_bitrate, song_file_size, song_last_modified, song_file_name, song_release_year, song_art_id)" + 
					"VALUES (@folderid, @artistid, @albumid, @filetype, @songname, @tracknum, @discnum, @duration, @bitrate, @filesize, @lastmod, @filename, @releaseyear, @artid)"
				, conn);

				q.AddNamedParam("@folderid", FolderId);
				q.AddNamedParam("@artistid", ArtistId);
				q.AddNamedParam("@albumid", AlbumId);
				q.AddNamedParam("@filetype", (int)FileType);

				if (SongName == null)
					q.AddNamedParam("@songname", DBNull.Value);
				else
					q.AddNamedParam("@songname", SongName);

                if (TrackNumber == null)
                    q.AddNamedParam("@tracknum", DBNull.Value);
                else
                    q.AddNamedParam("@tracknum", TrackNumber);
				q.AddNamedParam("@discnum", DiscNumber);
				q.AddNamedParam("@duration", Duration);
				q.AddNamedParam("@bitrate", Bitrate);
				q.AddNamedParam("@filesize", FileSize);
				q.AddNamedParam("@lastmod", LastModified);
				q.AddNamedParam("@filename", FileName);
                if (ReleaseYear == null)
                    q.AddNamedParam("@releaseyear", DBNull.Value);
                else
                    q.AddNamedParam("@releaseyear", ReleaseYear);
                if (ArtId == null)
                    q.AddNamedParam("@artid", DBNull.Value);
                else
                    q.AddNamedParam("@artid", ArtId);

				q.Prepare();
				q.ExecuteNonQuery();
				return;
			}
			catch (Exception e)
			{
				Console.WriteLine("[SONG(3)] " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public static List<Song> allSongs()
		{
			var allsongs = new List<Song>();
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " +
					"LEFT JOIN artist ON song_artist_id = artist.artist_id " +
					"LEFT JOIN album ON song_album_id = album.album_id ", conn);

				q.Prepare();
				reader = q.ExecuteReader();

				//	var sw = new Stopwatch();
				while (reader.Read())
				{
					//		sw.Start();
					allsongs.Add(new Song(reader));
					//		Console.WriteLine("Elapsed: {0}ms", sw.ElapsedMilliseconds);
					//		sw.Restart();
				}
				//	sw.Stop();
			}
			catch (Exception e)
			{
				Console.WriteLine("[SONG(4)] " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return allsongs;
		}

		// stub!
		public static int CompareSongsByDiscAndTrack(Song x, Song y)
		{
			// if one track contains a disc number and the other doesn't, the one with the disc number is greater
			//if (x.DiscNumber == 0 && y.DiscNumber != 0) return 1;
			//else if (x.DiscNumber != 0 && y.DiscNumber == 0) return -1;

			if (x.DiscNumber == y.DiscNumber && x.TrackNumber == y.TrackNumber) return 0;

			// if the disc numbers are equal, we have to compare by track
			else if (x.DiscNumber == y.DiscNumber) return x.TrackNumber > y.TrackNumber ? 1 : -1;

			// if the disc numbers are not equal, the one with the higher disc number is greater.
			else return x.DiscNumber > y.DiscNumber ? 1 : -1;
		}
	}
}
