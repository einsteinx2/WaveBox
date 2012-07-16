using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class Artist
	{
		/// <summary>
		/// Properties
		/// </summary>
		/// 
		[JsonProperty("itemTypeId")]
		public long ItemTypeId
		{
			get
			{
				return ItemType.ARTIST.GetItemTypeId();
			}
		}

		[JsonProperty("artistId")]
		public long ArtistId { get; set; }

		[JsonProperty("artistName")]
		public string ArtistName { get; set; }

		[JsonProperty("artId")]
		public long ArtId { get; set; }


		/// <summary>
		/// Constructors
		/// </summary>
		
		public Artist()
		{
		}

		public Artist(SqliteDataReader reader)
		{
			SetPropertiesFromQueryResult(reader);
		}

		public Artist(long artistId)
		{
			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();

					var q = new SqliteCommand("SELECT * FROM artist WHERE artist_id = @artistid");
					q.Connection = conn;
					q.Parameters.AddWithValue("@artistid", artistId);
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						SetPropertiesFromQueryResult(reader);
					}
					else
					{
						Console.WriteLine("Artist constructor query returned no results");
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		public Artist(string artistName)
		{
			if (artistName == null || artistName == "")
			{
				return;
			}

			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();
					var q = new SqliteCommand("SELECT * FROM artist WHERE artist_name = @artistname");
					q.Connection = conn;
					q.Parameters.AddWithValue("@artistname", artistName);
					q.Prepare();
					reader = q.ExecuteReader();

					if (reader.Read())
					{
						SetPropertiesFromQueryResult(reader);
					}
					else
					{
						ArtistName = artistName;
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}
		}

		/// <summary>
		/// Private methods
		/// </summary>

		private void SetPropertiesFromQueryResult(SqliteDataReader reader)
		{
			try
			{
				ArtistId = reader.GetInt64(reader.GetOrdinal("artist_id"));
				ArtistName = reader.GetString(reader.GetOrdinal("artist_name"));

				if 
					(reader.GetValue(reader.GetOrdinal("artist_art_id")) == DBNull.Value) ArtId = 0;
				else 
					ArtId = reader.GetInt64(reader.GetOrdinal("artist_art_id"));
			}

			catch (SqliteException e)
			{
				if (e.InnerException.ToString() == "SqlNullValueException") { }
			}
		}

		private static bool InsertArtist(string artistName)
		{
			bool success = false;
			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();
					var q = new SqliteCommand("INSERT INTO artist (artist_name) VALUES (@artistname)");
					q.Connection = conn;
					q.Parameters.AddWithValue("@artistname", artistName);
					q.Prepare();
					int affected = q.ExecuteNonQuery();

					if (affected == 1)
					{
						success = true;
					}
					else
					{
						success = false;
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}

			return success;
		}

		/// <summary>
		/// Public methods
		/// </summary>

		public List<Album> ListOfAlbums()
		{
			var albums = new List<Album>();

			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					conn = Database.GetDbConnection();
					var q = new SqliteCommand("SELECT * FROM album WHERE artist_id = @artistid");
					q.Connection = conn;
					q.Parameters.AddWithValue("@artistid", ArtistId);
					q.Prepare();
					reader = q.ExecuteReader();

					while (reader.Read())
					{
						albums.Add(new Album(reader));
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}

			albums.Sort(Album.CompareAlbumsByName);
			return albums;
		}

		public List<Song> ListOfSongs()
		{
			var songs = new List<Song>();


			SqliteConnection conn = null;
			SqliteDataReader reader = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " + 
						"LEFT JOIN artist ON song_artist_id = artist_id " +
						"LEFT JOIN album ON song_album_id = album_id " +
						"WHERE song_artist_id = @artistid"
					);

					q.Parameters.AddWithValue("@artistid", ArtistId);

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					reader = q.ExecuteReader();

					while (reader.Read())
					{
						songs.Add(new Song(reader));
					}

					reader.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				finally
				{
					Database.Close(conn, reader);
				}
			}

			songs.Sort(Song.CompareSongsByDiscAndTrack);
			return songs;
		}

		public static Artist ArtistForName(string artistName)
		{
			if (artistName == null || artistName == "")
			{
				return new Artist();
			}

			// check to see if the artist exists
			var anArtist = new Artist(artistName);

			// if not, create it.
			if (anArtist.ArtistId == 0)
			{
				anArtist = null;
				if (InsertArtist(artistName))
				{
					anArtist = ArtistForName(artistName);
				}
			}

			// then return the artist object retrieved or created.
			return anArtist;
		}

		public List<Artist> AllArtists()
		{
			var artists = new List<Artist>();

			SqliteConnection conn = null;
			SqliteDataReader result = null;

			lock (Database.dbLock)
			{
				try
				{
					var q = new SqliteCommand("SELECT * FROM artist");

					conn = Database.GetDbConnection();
					q.Connection = conn;
					q.Prepare();
					result = q.ExecuteReader();

					while (result.Read())
					{
						artists.Add(new Artist(result));
					}

					result.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
				finally
				{
					Database.Close(conn, result);
				}
			}

			artists.Sort(Artist.CompareArtistsByName);

			return artists;
		}

		public static int CompareArtistsByName(Artist x, Artist y)
		{
			return StringComparer.OrdinalIgnoreCase.Compare(x.ArtistName, y.ArtistName);
		}
	}
}
