using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.ApiHandler;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class AlbumsApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "albums"; } }

		/// <summary>
		/// Process returns a serialized list of albums and songs in JSON format
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// List of songs and albums to be returned via handler
			IList<Song> songs = new List<Song>();
			IList<Album> albums = new List<Album>();

			// Fetch album ID from parameters
			int id = 0;
			if (uri.Parameters.ContainsKey("id"))
			{
				if (!Int32.TryParse(uri.Parameters["id"], out id))
				{
					string json = JsonConvert.SerializeObject(new AlbumsResponse("Parameter 'id' requires a valid integer", null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					processor.WriteJson(json);
					return;
				}

				// Add album by ID to the list
				Album a = Injection.Kernel.Get<IAlbumRepository>().AlbumForId(id);
				albums.Add(a);

				// Add album's songs to response
				songs = a.ListOfSongs();
			}
			// Check for a request for range of songs
			else if (uri.Parameters.ContainsKey("range"))
			{
				string[] range = uri.Parameters["range"].Split(',');

				// Ensure valid range was parsed
				if (range.Length != 2)
				{
					string json = JsonConvert.SerializeObject(new AlbumsResponse("Parameter 'range' requires a valid, comma-separated character tuple", null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					processor.WriteJson(json);
					return;
				}

				// Validate as characters
				char start, end;
				if (!Char.TryParse(range[0], out start) || !Char.TryParse(range[1], out end))
				{
					string json = JsonConvert.SerializeObject(new AlbumsResponse("Parameter 'range' requires characters which are single alphanumeric values", null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					processor.WriteJson(json);
					return;
				}

				// Grab range of albums
				albums = Injection.Kernel.Get<IAlbumRepository>().RangeAlbums(start, end);
			}

			// Check for a request to limit/paginate songs, like SQL
			// Note: can be combined with range or all albums
			if (uri.Parameters.ContainsKey("limit") && !uri.Parameters.ContainsKey("id"))
			{
				string[] limit = uri.Parameters["limit"].Split(',');

				// Ensure valid limit was parsed
				if (limit.Length < 1 || limit.Length > 2 )
				{
					string json = JsonConvert.SerializeObject(new AlbumsResponse("Parameter 'limit' requires a single integer, or a valid, comma-separated integer tuple", null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					processor.WriteJson(json);
					return;
				}

				// Validate as integers
				int index = 0;
				int duration = Int32.MinValue;
				if (!Int32.TryParse(limit[0], out index))
				{
					string json = JsonConvert.SerializeObject(new AlbumsResponse("Parameter 'limit' requires a valid integer start index", null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					processor.WriteJson(json);
					return;
				}

				// Ensure positive index
				if (index < 0)
				{
					string json = JsonConvert.SerializeObject(new AlbumsResponse("Parameter 'limit' requires a non-negative integer start index", null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					processor.WriteJson(json);
					return;
				}

				// Check for duration
				if (limit.Length == 2)
				{
					if (!Int32.TryParse(limit[1], out duration))
					{
						string json = JsonConvert.SerializeObject(new AlbumsResponse("Parameter 'limit' requires a valid integer duration", null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						processor.WriteJson(json);
						return;
					}

					// Ensure positive duration
					if (duration < 0)
					{
						string json = JsonConvert.SerializeObject(new AlbumsResponse("Parameter 'limit' requires a non-negative integer duration", null, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						processor.WriteJson(json);
						return;
					}
				}

				// Check if results list already populated by range
				if (albums.Count > 0)
				{
					// No duration?  Return just specified number of albums
					if (duration == Int32.MinValue)
					{
						albums = albums.Skip(0).Take(index).ToList();
					}
					else
					{
						// Else, return albums starting at index, up to count duration
						albums = albums.Skip(index).Take(duration).ToList();
					}
				}
				else
				{
					// If no albums in list, grab directly using model method
					albums = Injection.Kernel.Get<IAlbumRepository>().LimitAlbums(index, duration);
				}
			}

			// Finally, if no albums already in list, send the whole list
			if (albums.Count == 0)
			{
				albums = Injection.Kernel.Get<IAlbumRepository>().AllAlbums();
			}

			try
			{
				// Send it!
				string json = JsonConvert.SerializeObject(new AlbumsResponse(null, albums, songs), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
