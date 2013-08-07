using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class DatabaseApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "database"; } set { } }

		/// <summary>
		/// Process returns a copy of the media database, and can be used to return SQL deltas to update
		/// the local copy of the media database
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Try to get the update time
			string id = uri.Parameters.ContainsKey("id") ? uri.Parameters["id"] : null;

			if ((object)id == null)
			{
				// No id parameter, so send down the whole backup database
				long databaseLastQueryId = -1;
				string databaseFileName = DatabaseBackup.Backup(out databaseLastQueryId);

				if ((object)databaseFileName == null)
				{
					processor.WriteErrorHeader();
				}
				else
				{
					try
					{
						// Read in entire database file
						Stream stream = new FileStream(ServerUtility.RootPath() + databaseFileName, FileMode.Open, FileAccess.Read);
						long length = stream.Length;
						int startOffset = 0;

						// Handle the Range header to start from later in the file if connection interrupted
						if (processor.HttpHeaders.ContainsKey("Range"))
						{
							string range = (string)processor.HttpHeaders["Range"];
							string start = range.Split(new char[]{'-', '='})[1];
							logger.IfInfo("Connection retried.  Resuming from " + start);
							startOffset = Convert.ToInt32(start);
						}

						// We send the last query id as a custom header
						IDictionary<string, string> customHeader = new Dictionary<string, string>();
						customHeader["WaveBox-LastQueryId"] = databaseLastQueryId.ToString();

						// Send the database file
						processor.WriteFile(stream, startOffset, length, "application/octet-stream", customHeader, true, new FileInfo(ServerUtility.RootPath() + databaseFileName).LastWriteTimeUtc);
                        stream.Close();
					}
					catch
					{
						// Send JSON on error
						string json = JsonConvert.SerializeObject(new DatabaseResponse("Could not open backup database " + databaseFileName, null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						processor.WriteJson(json);
					}
				}
			}
			else
			{
				// Return all queries >= this id
				try
				{
					// Send DatabaseResponse containing list of queries
					string json = JsonConvert.SerializeObject(new DatabaseResponse(null, Injection.Kernel.Get<IDatabase>().QueryLogsSinceId(Int32.Parse(id))), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
					processor.WriteJson(json);
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
			}
		}
	}
}
