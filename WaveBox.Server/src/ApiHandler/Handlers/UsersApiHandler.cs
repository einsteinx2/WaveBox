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

		public string Name { get { return "users"; } }

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
				if (testUser == null)
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

					return;
				}

				listOfUsers.Add(testUser);
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

						return;
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

						return;
					}

					// After all checks pass, delete this session, return user associated with it
					var session = Injection.Kernel.Get<ISessionRepository>().SessionForRowId(rowId);
					if (session != null)
					{
						session.DeleteSession();
						listOfUsers.Add(Injection.Kernel.Get<IUserRepository>().UserForId(Convert.ToInt32(session.UserId)));
					}
				}
				// create - create a new user
				else if (uri.Parameters["action"] == "create")
				{
					// Check for admin permission
					if (!user.HasPermission(User.ROLE_ADMIN))
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("Permission denied for action 'create'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
							processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}

						return;
					}

					// Check for required username and password parameters
					if (!uri.Parameters.ContainsKey("username") || !uri.Parameters.ContainsKey("password"))
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("Parameters 'username' and 'password' required for action 'create'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
							processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}

						return;
					}

					string username = uri.Parameters["username"];
					string password = uri.Parameters["password"];

					// Attempt to create the user, with regular user role for now
					User newUser = Injection.Kernel.Get<IUserRepository>().CreateUser(username, password, User.ROLE_USER, null);
					if (newUser == null)
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("Action 'create' failed to create new user", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
							processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}

						return;
					}

					// Verify user didn't already exist
					if (newUser.UserId == null)
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("User '" + username + "' already exists!", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
							processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}

						return;
					}

					// Return newly created user
					logger.IfInfo(String.Format("Successfully created new user [id: {0}, username: {1}]", newUser.UserId, newUser.UserName));
					listOfUsers.Add(newUser);
				}
				else if (uri.Parameters["action"] == "delete")
				{
					// Check for required ID parameter
					if (!uri.Parameters.ContainsKey("id"))
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("Parameter 'id' is required for action 'delete'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
							processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}

						return;
					}

					// Try to parse id integer
					int id = 0;
					if (!Int32.TryParse(uri.Parameters["id"], out id))
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("Invalid integer for 'id' for action 'delete'", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
							processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}

						return;
					}

					// Attempt to fetch and delete user
					User deleteUser = Injection.Kernel.Get<IUserRepository>().UserForId(id);
					if (deleteUser.UserName == null || !deleteUser.Delete())
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("Action 'delete' failed to delete user", null), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
							processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}

						return;
					}

					// Return deleted user
					logger.IfInfo(String.Format("Successfully deleted user [id: {0}, username: {1}]", deleteUser.UserId, deleteUser.UserName));
					listOfUsers.Add(deleteUser);
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

					return;
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
			}

			// Finally, output user information
			try
			{
				string json = JsonConvert.SerializeObject(new UsersResponse(null, listOfUsers), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}

			return;
		}
	}
}
