using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using WaveBox.DataModel.Singletons;
using Newtonsoft.Json;

namespace WaveBox.DataModel.Model
{
	public class Artist : IItem
	{
		[JsonIgnore]
		public int? ItemId { get { return ArtistId; } set { ArtistId = ItemId; } }

		[JsonIgnore]
		public ItemType ItemType { get { return ItemType.Artist; } }

		[JsonProperty("itemTypeId")]
		public int ItemTypeId { get { return (int)ItemType; } }

		[JsonProperty("artistId")]
		public int? ArtistId { get; set; }

		[JsonProperty("artistName")]
		public string ArtistName { get; set; }

		[JsonProperty("artId")]
		public int? ArtId { get { return Art.ArtIdForItemId(ArtistId); } }


		/// <summary>
		/// Constructors
		/// </summary>
		
		public Artist()
		{
		}

		public Artist(IDataReader reader)
		{
			SetPropertiesFromQueryReader(reader);
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
					SetPropertiesFromQueryReader(reader);
				}
				else
				{
					Console.WriteLine("Artist constructor query returned no results");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ARTIST(1)] ERROR: " +  e);
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
					SetPropertiesFromQueryReader(reader);
				}
				else
				{
					ArtistName = artistName;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ARTIST(2)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
			}
		}

		/// <summary>
		/// Private methods
		/// </summary>

		private void SetPropertiesFromQueryReader(IDataReader reader)
		{
			ArtistId = reader.GetInt32OrNull(reader.GetOrdinal("artist_id"));
			ArtistName = reader.GetStringOrNull(reader.GetOrdinal("artist_name"));
		}

		private static bool InsertArtist(string artistName)
		{
			int? itemId = Item.GenerateItemId(ItemType.Artist);
			if (itemId == null)
				return false;
			
			bool success = false;
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT OR IGNORE INTO artist (artist_id, artist_name) VALUES (@artistid, @artistname)", conn);
				q.AddNamedParam("@artistid", itemId);
				q.AddNamedParam("@artistname", artistName);
				q.Prepare();
				int affected = q.ExecuteNonQueryLogged();

				if (affected == 1)
				{
					success = true;
				}
				else
					success = false;
			}
			catch (Exception e)
			{
				Console.WriteLine("[ARTIST(4)] ERROR: " + e);
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
			List<Album> albums = new List<Album>();
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
				Console.WriteLine("[ARTIST(5)] ERROR: " + e);
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
			List<Song> songs = new List<Song>();
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
				Console.WriteLine("[ARTIST(6)] ERROR: " + e);
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
			Artist anArtist = new Artist(artistName);

			// if not, create it.
			if (anArtist.ArtistId == null)
			{
				anArtist = null;
				if (InsertArtist(artistName))
				{
					anArtist = ArtistForName(artistName);
				}
				else 
				{
					// The insert failed because this album was inserted by another
					// thread, so grab the album id, it will exist this time
					anArtist = new Artist(artistName);
				}
			}

			// then return the artist object retrieved or created.
			return anArtist;
		}

		public List<Artist> AllArtists()
		{
			List<Artist> artists = new List<Artist>();

			IDbConnection conn = null;
			IDataReader reader = null;

			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT * FROM artist", conn);
				q.Prepare();
				reader = q.ExecuteReader();

				while (reader.Read())
				{
					artists.Add(new Artist(reader));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[ARTIST(7)] ERROR: " + e);
			}
			finally
			{
				Database.Close(conn, reader);
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
