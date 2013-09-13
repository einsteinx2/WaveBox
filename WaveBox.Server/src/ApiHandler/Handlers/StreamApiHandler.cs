using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Model;
using WaveBox.Server.Extensions;
using WaveBox.Service;
using WaveBox.Service.Services;
using WaveBox.Service.Services.Http;
using WaveBox.Static;
using WaveBox.Transcoding;

namespace WaveBox.ApiHandler.Handlers
{
	public class StreamApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "stream"; } }

		/// <summary>
		/// Process produces a direct file stream of the requested media file
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			logger.IfInfo("Starting file streaming sequence");

			// Try to get the media item id
			int id = 0;
			if (uri.Parameters.ContainsKey("id"))
			{
				Int32.TryParse(uri.Parameters["id"], out id);
			}

			float seconds = 0f;
			if (uri.Parameters.ContainsKey("seconds"))
			{
				float.TryParse(uri.Parameters["seconds"], out seconds);
			}

			if (id == 0)
			{
				// For missing ID parameter, print JSON error
				processor.WriteJson(new StreamResponse("Missing required parameter 'id'"));
				return;
			}

			try
			{
				// Get the media item associated with this id
				ItemType itemType = Injection.Kernel.Get<IItemRepository>().ItemTypeForItemId(id);
				IMediaItem item = null;
				if (itemType == ItemType.Song)
				{
					item = Injection.Kernel.Get<ISongRepository>().SongForId(id);
					logger.IfInfo("Preparing audio stream: " + item.FileName);
				}
				else if (itemType == ItemType.Video)
				{
					item = Injection.Kernel.Get<IVideoRepository>().VideoForId(id);
					logger.IfInfo("Preparing video stream: " + item.FileName);
				}

				// Return an error if none exists
				if ((item == null) || (!File.Exists(item.FilePath())))
				{
					processor.WriteJson(new StreamResponse("No media item exists with ID: " + id));
					return;
				}

				// Prepare file stream
				Stream stream = item.File();
				long length = stream.Length;
				int startOffset = 0;
				long? limitToSize = null;

				if (seconds > 0)
				{
					// Guess the file position based on the seconds requested
					// this is just rough now, but will be improved to take into account the header size
					float percent = seconds / (float)item.Duration;
					if (percent < 1f)
					{
						startOffset = (int)(item.FileSize * percent);
						logger.IfInfo("seconds: " + seconds + "  Resuming from " + startOffset);
					}
				}
				else if (processor.HttpHeaders.ContainsKey("Range"))
				{
					// Handle the Range header to start from later in the file
					string range = (string)processor.HttpHeaders["Range"];
					var split = range.Split(new char[]{'-', '='});
					string start = split[1];
					string end = split.Length > 2 ? split[2] : null;

					logger.IfInfo("Range header: " + range + "  Resuming from " + start);
					startOffset = Convert.ToInt32(start);
					if (!ReferenceEquals(end, null))
					{
						limitToSize = (Convert.ToInt64(end) + 1) - startOffset;
					}
				}

				// Send the file
				processor.WriteFile(stream, startOffset, length, item.FileType.MimeType(), null, true, new FileInfo(item.FilePath()).LastWriteTimeUtc, limitToSize);
				stream.Close();

				logger.IfInfo("Successfully streamed file!");
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}
	}
}
