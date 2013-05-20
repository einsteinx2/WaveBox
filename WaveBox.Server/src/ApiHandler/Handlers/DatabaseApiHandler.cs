using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Data;
using System.IO;
using WaveBox.Static;
using WaveBox.Model;
using WaveBox.TcpServer.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class DatabaseApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		
		/// <summary>
		/// Constructor for DatabaseApiHandler class
		/// </summary>
		public DatabaseApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
		}
		
		/// <summary>
		/// Process returns a copy of the media database, and can be used to return SQL deltas to update
		/// the local copy of the media database
		/// </summary>
		public void Process()
		{
			// Try to get the update time
			string id = Uri.Parameters.ContainsKey("id") ? Uri.Parameters["id"] : null;

			if ((object)id == null)
			{
				// No id parameter, so send down the whole backup database
				long databaseLastQueryId = -1;
				string databaseFileName = Database.Backup(out databaseLastQueryId);

				if ((object)databaseFileName == null)
				{
					Processor.WriteErrorHeader();
				}
				else
				{
					try
					{
						// Read in entire database file
						Stream stream = new FileStream(Utility.RootPath() + databaseFileName, FileMode.Open, FileAccess.Read);
						long length = stream.Length;
						int startOffset = 0;
					
						// Handle the Range header to start from later in the file if connection interrupted
						if (Processor.HttpHeaders.ContainsKey("Range"))
						{
							string range = (string)Processor.HttpHeaders["Range"];
							string start = range.Split(new char[]{'-', '='})[1];
							if (logger.IsInfoEnabled) logger.Info("Connection retried.  Resuming from " + start);
							startOffset = Convert.ToInt32(start);
						}

						// We send the last query id as a custom header
						IDictionary<string, string> customHeader = new Dictionary<string, string>();
						customHeader["WaveBox-LastQueryId"] = databaseLastQueryId.ToString();
					
						// Send the database file
						Processor.WriteFile(stream, startOffset, length, "application/octet-stream", customHeader, true);
                        stream.Close();
					}
					catch
					{
						// Send JSON on error
						string json = JsonConvert.SerializeObject(new DatabaseResponse("Could not open backup database " + databaseFileName, null), Settings.JsonFormatting);
						Processor.WriteJson(json);
					}
				}
			}
			else
			{
				// Return all queries >= this id
				IDbConnection conn = null;
				IDataReader reader = null;
				IList<IDictionary<string, object>> queries = new List<IDictionary<string, object>>();
				try
				{
					// Gather a list of queries from the query log, which can be used to synchronize a local database
					conn = Database.GetQueryLogDbConnection();
					IDbCommand q = Database.GetDbCommand("SELECT * FROM query_log WHERE query_id >= @queryid", conn);
					q.AddNamedParam("@queryid", id);
					q.Prepare();
					reader = q.ExecuteReader();
					
					// Read in queries and pull query data into list
					while (reader.Read())
					{
						long queryId = reader.GetInt64(0);
						string queryString = reader.GetString(1);
						string values = reader.GetString(2);

						// Generate dictionary containing delta of queries, add to list
						IDictionary<string, object> query = new Dictionary<string, object>();
						query["id"] = queryId;
						query["query"] = queryString;
						query["values"] = values;

						queries.Add(query);
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					// Ensure database closed
					Database.Close(conn, null);
				}

				try
				{
					// Send DatabaseResponse containing list of queries
					string json = JsonConvert.SerializeObject(new DatabaseResponse(null, queries), Settings.JsonFormatting);
					Processor.WriteJson(json);
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
			}
		}

		private class DatabaseResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }
			
			[JsonProperty("queries")]
			public IList<IDictionary<string, object>> Queries { get; set; }

			public DatabaseResponse(string error, IList<IDictionary<string, object>> queries)
			{
				Error = error;
				Queries = queries;
			}
		}
	}
}