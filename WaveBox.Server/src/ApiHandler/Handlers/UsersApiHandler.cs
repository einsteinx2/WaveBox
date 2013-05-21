using System;
using System.Collections.Generic;
using WaveBox.Static;
using WaveBox.Model;
using Newtonsoft.Json;

namespace WaveBox.ApiHandler.Handlers
{
	public class UsersApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private User User { get; set; }

		/// <summary>
		/// Constructors for UsersApiHandler
		/// </summary>
		public UsersApiHandler(UriWrapper uri, IHttpProcessor processor, User user)
		{
			Processor = processor;
			Uri = uri;
			User = user;
		}

		/// <summary>
		/// Process allows the modification of users and their properties
		/// </summary>
		public void Process()
		{
			// Return list of users to be passed as JSON
			List<User> listOfUsers = new List<User>();

			// See if we need to make a test user
			if (Uri.Parameters.ContainsKey("testUser") && Uri.Parameters["testUser"].IsTrue())
			{
				bool success = false;
				int durationSeconds = 0;
				if (Uri.Parameters.ContainsKey("durationSeconds"))
				{
					success = Int32.TryParse(Uri.Parameters["durationSeconds"], out durationSeconds);
				}

				// Create a test user and reply with the account info
				User testUser = User.CreateTestUser(success ? (int?)durationSeconds : null);
				if (!ReferenceEquals(testUser, null))
				{
					listOfUsers.Add(testUser);
					try
					{
						string json = JsonConvert.SerializeObject(new UsersResponse(null, listOfUsers), Settings.JsonFormatting);
						Processor.WriteJson(json);
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
						string json = JsonConvert.SerializeObject(new UsersResponse("Couldn't create user", listOfUsers), Settings.JsonFormatting);
						Processor.WriteJson(json);
					}
					catch (Exception e)
					{
						logger.Error(e);
					}
				}
			}
			// See if we need to manage users
			else if (Uri.Parameters.ContainsKey("action"))
			{
				// killSession - remove a session by rowId
				if (Uri.Parameters["action"] == "killSession")
				{
					// Try to pull rowId from parameters for session management
					int rowId = 0;
					if (!Uri.Parameters.ContainsKey("rowId"))
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("Missing parameter 'rowId' for action 'killSession'", null), Settings.JsonFormatting);
							Processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}
					}

					// Try to parse rowId integer
					if (!Int32.TryParse(Uri.Parameters["rowId"], out rowId))
					{
						try
						{
							string json = JsonConvert.SerializeObject(new UsersResponse("invalid integer for 'rowId' for action 'killSession'", null), Settings.JsonFormatting);
							Processor.WriteJson(json);
						}
						catch (Exception e)
						{
							logger.Error(e);
						}
					}

					// After all checks pass, delete this session, return user associated with it
					var session = new Session(rowId);
					if (session != null)
					{
						session.DeleteSession();
						listOfUsers.Add(new User(Convert.ToInt32(session.UserId)));
					}

					try
					{
						string json = JsonConvert.SerializeObject(new UsersResponse(null, listOfUsers), Settings.JsonFormatting);
						Processor.WriteJson(json);
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
				if (Uri.Parameters.ContainsKey("id"))
				{
					success = Int32.TryParse(Uri.Parameters["id"], out id);
				}

				// On valid key, return a specific user, and their attributes
				if (success)
				{
					User user = new User(id);
					listOfUsers.Add(user);
				}
				else
				{
					// On invalid key, return all users
					listOfUsers = User.AllUsers();
				}

				try
				{
					string json = JsonConvert.SerializeObject(new UsersResponse(null, listOfUsers), Settings.JsonFormatting);
					Processor.WriteJson(json);
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
			}
		}

		private class UsersResponse
		{
			[JsonProperty("error")]
			public string Error { get; set; }

			[JsonProperty("users")]
			public List<User> Users { get; set; }

			public UsersResponse(string error, List<User> users)
			{
				Error = error;
				Users = users;
			}
		}
	}
}

