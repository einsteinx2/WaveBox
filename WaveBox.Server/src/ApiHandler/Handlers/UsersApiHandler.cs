using System;
using System.Collections.Generic;
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
	public class UsersApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "users"; } set { } }

		/// <summary>
		/// Process allows the modification of users and their properties
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Return list of users to be passed as JSON
			IList<User> listOfUsers = new List<User>();

			// See if we need to make a test user
			if (uri.Parameters.ContainsKey("testUser") && uri.Parameters["testUser"].IsTrue())
			{
				bool success = false;
				int durationSeconds = 0;
				if (uri.Parameters.ContainsKey("durationSeconds"))
				{
					success = Int32.TryParse(uri.Parameters["durationSeconds"], out durationSeconds);
				}

				// Create a test user and reply with the account info
				User testUser = Injection.Kernel.Get<IUserRepository>().CreateTestUser(success ? (int?)durationSeconds : null);
				if (!ReferenceEquals(testUser, null))
				{
					listOfUsers.Add(testUser);
					try
					{
						string json = JsonConvert.SerializeObject(new UsersResponse(null, listOfUsers), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						processor.WriteJson(json);
					}
					catch (Exception e)
					{
						logger.Error(e);
					}
				}
				else
				{
					try
					{
						string json = JsonConvert.SerializeObject(new UsersResponse("Couldn't create user", listOfUsers), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						processor.WriteJson(json);
					}
					catch (Exception e)
					{
						logger.Error(e);
					}
				}
			}
			// See if we need to manage users
			else if (uri.Parameters.ContainsKey("action"))
			{
				// killSession - remove a session by rowId
				if (uri.Parameters["action"] == "killSession")
				{
					// Try to pull rowId from parameters for session management
					int rowId = 0;
					if (!uri.Parameters.ContainsKey("rowId"))
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("Missing parameter 'rowId' for action 'killSession'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
							processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}
					}

					// Try to parse rowId integer
					if (!Int32.TryParse(uri.Parameters["rowId"], out rowId))
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("Invalid integer for 'rowId' for action 'killSession'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
							processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}
					}

					// After all checks pass, delete this session, return user associated with it
					var session = Injection.Kernel.Get<ISessionRepository>().SessionForRowId(rowId);
					if (session != null)
					{
						session.DeleteSession();
						listOfUsers.Add(Injection.Kernel.Get<IUserRepository>().UserForId(Convert.ToInt32(session.UserId)));
					}

					try
					{
						string json = JsonConvert.SerializeObject(new UsersResponse(null, listOfUsers), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						processor.WriteJson(json);
					}
					catch (Exception e)
					{
						logger.Error(e);
					}
				}
				else
				{
					// Invalid action
					try
					{
						string json = JsonConvert.SerializeObject(new UsersResponse("Invalid action '" + uri.Parameters["action"] + "'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
						processor.WriteJson(json);
					}
					catch (Exception e)
					{
						logger.Error(e);
					}
				}
			}
			// Else, list user information
			else
			{
				// Try to get a user ID
				bool success = false;
				int id = 0;
				if (uri.Parameters.ContainsKey("id"))
				{
					success = Int32.TryParse(uri.Parameters["id"], out id);
				}

				// On valid key, return a specific user, and their attributes
				if (success)
				{
					User oneUser = Injection.Kernel.Get<IUserRepository>().UserForId(id);
					listOfUsers.Add(oneUser);
				}
				else
				{
					// On invalid key, return all users
					listOfUsers = Injection.Kernel.Get<IUserRepository>().AllUsers();
				}

				try
				{
					string json = JsonConvert.SerializeObject(new UsersResponse(null, listOfUsers), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
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
