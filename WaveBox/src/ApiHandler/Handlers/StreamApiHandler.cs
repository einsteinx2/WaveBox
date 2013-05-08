﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;
using WaveBox.TcpServer.Http;
using WaveBox.Transcoding;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	public class StreamApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }

		/// <summary>
		/// Constructor for StreamApiHandler
		/// </summary>
		public StreamApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}

		/// <summary>
		/// Process produces a direct file stream of the requested media file
		/// </summary>
		public void Process()
		{
			if (logger.IsInfoEnabled) logger.Info("Starting file streaming sequence");

			// Try to get the media item id
			bool success = false;
			int id = 0;
			if (Uri.Parameters.ContainsKey("id"))
			{
				success = Int32.TryParse(Uri.Parameters["id"], out id);
			}

			if (success)
			{
				try
				{
					// Get the media item associated with this id
					ItemType itemType = Item.ItemTypeForItemId(id);
					IMediaItem item = null;
					if (itemType == ItemType.Song)
					{
						item = new Song(id);
						if (logger.IsInfoEnabled) logger.Info("Preparing audio stream: " + item.FileName);
					}
					else if (itemType == ItemType.Video)
					{
						item = new Video(id);
						if (logger.IsInfoEnabled) logger.Info("Preparing video stream: " + item.FileName);
					}

					// Return an error if none exists
					if ((item == null) || (!File.Exists(item.FilePath)))
					{
						string json = JsonConvert.SerializeObject(new StreamResponse("No media item exists with ID: " + id), Settings.JsonFormatting);
						Processor.WriteJson(json);
						return;
					}

					// Prepare file stream
					Stream stream = item.File;
					long length = stream.Length;
					int startOffset = 0;

					// Handle the Range header to start from later in the file
					if (Processor.HttpHeaders.ContainsKey("Range"))
					{
						string range = (string)Processor.HttpHeaders["Range"];
						string start = range.Split(new char[]{'-', '='})[1];

						if (logger.IsInfoEnabled) logger.Info("Connection retried.  Resuming from " + start);
						startOffset = Convert.ToInt32(start);
					}

					// Write additional file headers
					var dict = new Dictionary<string, string>();
					var lmt = HttpProcessor.DateTimeToLastMod(new FileInfo(item.FilePath).LastWriteTimeUtc);
					dict.Add("Last-Modified", lmt);

					// Send the file
					Processor.WriteFile(stream, startOffset, length, item.FileType.MimeType(), dict, true);
					stream.Close();
					
					if (logger.IsInfoEnabled) logger.Info("Successfully streamed file!");
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
			}
			else
			{
				// For missing ID parameter, print JSON error
				string json = JsonConvert.SerializeObject(new StreamResponse("Missing required parameter 'id'"), Settings.JsonFormatting);
				Processor.WriteJson(json);
			}
		}
		
		private class StreamResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }
			
			public StreamResponse(string error)
			{
				Error = error;
			}
		}
	}
}
