using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
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
		public int ItemTypeId { get { return ItemType.ARTIST.ItemTypeId(); } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("artistName")]
		public string ArtistName { get; set; }

		[JsonProperty("artId")]
		public int? ArtId { get; set; }


		/// <summary>
		/// Constructors
		/// </summary>
		
		public Artist()
		{
		}

		public Artist(IDataReader reader)
		{
			SetPropertiesFromQueryResult(reader);
		}

		public Artist(int? artistId)
		{
            if(artistId == null) return;

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();

				IDbCommand q = Database.GetDbCommand("SELECT * FROM artist WHERE artist_id = @artistid", conn);
				q.AddNamedParam("@artistid", artistId);
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
				Console.WriteLine("[ARTIST(1)] ERROR: " +  e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		public Artist(string artistName)
		{
			if (artistName == null || artistName == "")
			{
				return;
			}

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM artist WHERE artist_name = @artistname", conn);
				q.AddNamedParam("@artistname", artistName);
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
				Console.WriteLine("[ARTIST(2)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		/// <summary>
		/// Private methods
		/// </summary>

		private void SetPropertiesFromQueryResult(IDataReader reader)
		{
			try
			{
				ArtistId = reader.GetInt32(reader.GetOrdinal("artist_id"));
				ArtistName = reader.GetString(reader.GetOrdinal("artist_name"));

				if 
					(reader.GetValue(reader.GetOrdinal("artist_art_id")) == DBNull.Value) ArtId = null;
				else 
					ArtId = reader.GetInt32(reader.GetOrdinal("artist_art_id"));
			}
			catch (Exception e)
			{
				if (e.InnerException.ToString() == "SqlNullValueException") { }
				Console.WriteLine("[ARTIST(3)] ERROR: " + e.ToString());
			}
		}

		private static bool InsertArtist(string artistName)
		{
			bool success = false;
			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT OR IGNORE INTO artist (artist_name) VALUES (@artistname)", conn);
				q.AddNamedParam("@artistname", artistName);
				q.Prepare();
				int affected = q.ExecuteNonQuery();

				if (affected == 1)
				{
					success = true;
				}
				else
					success = false;
			}
			catch (Exception e)
			{
				Console.WriteLine("[ARTIST(4)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return success;
		}

		/// <summary>
		/// Public methods
		/// </summary>

		public List<Album> ListOfAlbums()
		{
			var albums = new List<Album>();

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM album WHERE artist_id = @artistid", conn);
				q.AddNamedParam("@artistid", ArtistId);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					albums.Add(new Album(reader));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ARTIST(5)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
			}

			albums.Sort(Album.CompareAlbumsByName);
			return albums;
		}

		public List<Song> ListOfSongs()
		{
			var songs = new List<Song>();


			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT song.*, artist.artist_name, album.album_name FROM song " + 
					"LEFT JOIN artist ON song_artist_id = artist.artist_id " +
					"LEFT JOIN album ON song_album_id = album.album_id " +
					"WHERE song_artist_id = @artistid", conn);
				q.AddNamedParam("@artistid", ArtistId);

				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					songs.Add(new Song(reader));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ARTIST(6)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, reader);
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
			if (anArtist.ArtistId == null)
			{
				anArtist = null;
				InsertArtist(artistName);
				anArtist = ArtistForName(artistName);
			}

			// then return the artist object retrieved or created.
			return anArtist;
		}

		public List<Artist> AllArtists()
		{
			var artists = new List<Artist>();

			IDbConnection conn = null;
			IDataReader result = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM artist", conn);
				q.Prepare();
				result = q.ExecuteReader();

				while (result.Read())
				{
					artists.Add(new Artist(result));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ARTIST(7)] ERROR: " + e.ToString());
			}
			finally
			{
				Database.Close(conn, result);
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
