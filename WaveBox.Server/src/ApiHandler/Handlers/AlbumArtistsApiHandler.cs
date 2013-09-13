using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Static;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class AlbumArtistsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "albumartists"; } }

		/// <summary>
		/// Process returns an AlbumArtistsResponse containing a list of artists, albums, and songs
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Lists of artists, albums, songs to be returned via handler
			IList<AlbumArtist> albumArtists = new List<AlbumArtist>();
			IList<Album> albums = new List<Album>();
			IList<Song> songs = new List<Song>();

			// Optional Last.fm info
			string lastfmInfo = null;

			// Check if an ID was passed
			if (uri.Id != null)
			{
				// Add artist by ID to the list
				AlbumArtist a = Injection.Kernel.Get<IAlbumArtistRepository>().AlbumArtistForId(uri.Id);
				albumArtists.Add(a);

				// Add artist's albums to response
				albums = a.ListOfAlbums();

				// If requested, add artist's songs to response
				if (uri.Parameters.ContainsKey("includeSongs"))
				{
					if (uri.Parameters["includeSongs"].IsTrue())
					{
						songs = a.ListOfSongs();
					}
				}

				// If requested, add artist's Last.fm info to response
				if (uri.Parameters.ContainsKey("lastfmInfo"))
				{
					if (uri.Parameters["lastfmInfo"].IsTrue())
					{
						logger.IfInfo("Querying Last.fm for artist: " + a.AlbumArtistName);
						try
						{
							lastfmInfo = Lastfm.GetAlbumArtistInfo(a);
							logger.IfInfo("Last.fm query complete!");
						}
						catch (Exception e)
						{
							logger.Error("Last.fm query failed!");
							logger.Error(e);
						}
					}
				}
			}
			// Check for a request for range of artists
			else if (uri.Parameters.ContainsKey("range"))
			{
				string[] range = uri.Parameters["range"].Split(',');

				// Ensure valid range was parsed
				if (range.Length != 2)
				{
					processor.WriteJson(new AlbumArtistsResponse("Parameter 'range' requires a valid, comma-separated character tuple", null, null, null));
					return;
				}

				// Validate as characters
				char start, end;
				if (!Char.TryParse(range[0], out start) || !Char.TryParse(range[1], out end))
				{
					processor.WriteJson(new AlbumArtistsResponse("Parameter 'range' requires characters which are single alphanumeric values", null, null, null));
					return;
				}

				// Grab range of artists
				albumArtists = Injection.Kernel.Get<IAlbumArtistRepository>().RangeAlbumArtists(start, end);
			}

			// Check for a request to limit/paginate artists, like SQL
			// Note: can be combined with range or all artists
			if (uri.Parameters.ContainsKey("limit") && uri.Id == null)
			{
				string[] limit = uri.Parameters["limit"].Split(',');

				// Ensure valid limit was parsed
				if (limit.Length < 1 || limit.Length > 2 )
				{
					processor.WriteJson(new AlbumArtistsResponse("Parameter 'limit' requires a single integer, or a valid, comma-separated integer tuple", null, null, null));
					return;
				}

				// Validate as integers
				int index = 0;
				int duration = Int32.MinValue;
				if (!Int32.TryParse(limit[0], out index))
				{
					processor.WriteJson(new AlbumArtistsResponse("Parameter 'limit' requires a valid integer start index", null, null, null));
					return;
				}

				// Ensure positive index
				if (index < 0)
				{
					processor.WriteJson(new AlbumArtistsResponse("Parameter 'limit' requires a non-negative integer start index", null, null, null));
					return;
				}

				// Check for duration
				if (limit.Length == 2)
				{
					if (!Int32.TryParse(limit[1], out duration))
					{
						processor.WriteJson(new AlbumArtistsResponse("Parameter 'limit' requires a valid integer duration", null, null, null));
						return;
					}

					// Ensure positive duration
					if (duration < 0)
					{
						processor.WriteJson(new AlbumArtistsResponse("Parameter 'limit' requires a non-negative integer duration", null, null, null));
						return;
					}
				}

				// Check if results list already populated by range
				if (albumArtists.Count > 0)
				{
					// No duration?  Return just specified number of artists
					if (duration == Int32.MinValue)
					{
						albumArtists = albumArtists.Skip(0).Take(index).ToList();
					}
					else
					{
						// Else, return artists starting at index, up to count duration
						albumArtists = albumArtists.Skip(index).Take(duration).ToList();
					}
				}
				else
				{
					// If no artists in list, grab directly using model method
					albumArtists = Injection.Kernel.Get<IAlbumArtistRepository>().LimitAlbumArtists(index, duration);
				}
			}

			// Finally, if no artists already in list, send the whole list
			if (albumArtists.Count == 0)
			{
				albumArtists = Injection.Kernel.Get<IAlbumArtistRepository>().AllAlbumArtists();
			}

			// Send it!
			processor.WriteJson(new AlbumArtistsResponse(null, albumArtists, albums, songs, lastfmInfo));
			return;
		}
	}
}
