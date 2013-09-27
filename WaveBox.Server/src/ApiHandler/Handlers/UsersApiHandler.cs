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

		// Standard permissions
		public bool CheckPermission(User user, string action)
		{
			switch (action)
			{
				// Admin
				case "create":
				case "delete":
				case "killSession":
				case "update":
					return user.HasPermission(Role.Admin);
				// Read
				case "read":
				default:
					return user.HasPermission(Role.Test);
			}
		}

		/// <summary>
		/// Process allows the modification of users and their properties
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			// Return list of users to be passed as JSON
			IList<User> listOfUsers = new List<User>();

			// Parse common parameters
			// Username
			string username = null;
			if (uri.Parameters.ContainsKey("username"))
			{
				username = uri.Parameters["username"];
			}

			// Password
			string password = null;
			if (uri.Parameters.ContainsKey("password"))
			{
				password = uri.Parameters["password"];
			}

			// Role
			Role role = Role.User;
			int roleInt = 0;
			if (uri.Parameters.ContainsKey("role") && Int32.TryParse(uri.Parameters["role"], out roleInt))
			{
				// Validate role
				if (Enum.IsDefined(typeof(Role), roleInt))
				{
					role = (Role)roleInt;
				}
			}

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
					processor.WriteJson(new UsersResponse("Couldn't create user", listOfUsers));
					return;
				}

				listOfUsers.Add(testUser);
				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// Check for no action, or read action
			if (uri.Action == null || uri.Action == "read")
			{
				// On valid key, return a specific user, and their attributes
				if (uri.Id != null)
				{
					User oneUser = Injection.Kernel.Get<IUserRepository>().UserForId((int)uri.Id);
					listOfUsers.Add(oneUser);
				}
				else
				{
					// Else, return all users
					listOfUsers = Injection.Kernel.Get<IUserRepository>().AllUsers();
				}

				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// Perform actions
			// killSession - remove a session by rowId
			if (uri.Action == "killSession")
			{
				// Try to pull rowId from parameters for session management
				int rowId = 0;
				if (!uri.Parameters.ContainsKey("rowId"))
				{
					processor.WriteJson(new UsersResponse("Missing parameter 'rowId' for action 'killSession'", null));
					return;
				}

				// Try to parse rowId integer
				if (!Int32.TryParse(uri.Parameters["rowId"], out rowId))
				{
					processor.WriteJson(new UsersResponse("Invalid integer for 'rowId' for action 'killSession'", null));
					return;
				}

				// After all checks pass, delete this session, return user associated with it
				var session = Injection.Kernel.Get<ISessionRepository>().SessionForRowId(rowId);
				if (session != null)
				{
					session.DeleteSession();
					listOfUsers.Add(Injection.Kernel.Get<IUserRepository>().UserForId(Convert.ToInt32(session.UserId)));
				}

				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// create - create a new user
			if (uri.Action == "create")
			{
				// Check for required username and password parameters
				if (username == null || password == null)
				{
					processor.WriteJson(new UsersResponse("Parameters 'username' and 'password' required for action 'create'", null));
					return;
				}

				// Attempt to create the user
				User newUser = Injection.Kernel.Get<IUserRepository>().CreateUser(username, password, role, null);
				if (newUser == null)
				{
					processor.WriteJson(new UsersResponse("Action 'create' failed to create new user", null));
					return;
				}

				// Verify user didn't already exist
				if (newUser.UserId == null)
				{
					processor.WriteJson(new UsersResponse("User '" + username + "' already exists!", null));
					return;
				}

				// Return newly created user
				logger.IfInfo(String.Format("Successfully created new user [id: {0}, username: {1}]", newUser.UserId, newUser.UserName));
				listOfUsers.Add(newUser);

				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// Verify ID present
			if (uri.Id == null)
			{
				processor.WriteJson(new UsersResponse("Missing parameter ID", null));
				return;
			}

			// delete - remove a user
			if (uri.Action == "delete")
			{
				// Attempt to fetch and delete user
				User deleteUser = Injection.Kernel.Get<IUserRepository>().UserForId((int)uri.Id);
				if (deleteUser.UserName == null || !deleteUser.Delete())
				{
					processor.WriteJson(new UsersResponse("Action 'delete' failed to delete user", null));
					return;
				}

				// Return deleted user
				logger.IfInfo(String.Format("Successfully deleted user [id: {0}, username: {1}]", deleteUser.UserId, deleteUser.UserName));
				listOfUsers.Add(deleteUser);

				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// update - update a user's username, password, or role
			if (uri.Action == "update")
			{
				// Attempt to get user
				User updateUser = Injection.Kernel.Get<IUserRepository>().UserForId((int)uri.Id);
				if (updateUser.UserName == null)
				{
					processor.WriteJson(new UsersResponse("Invalid user ID for action 'update'", null));
					return;
				}

				// Change username
				if (username != null)
				{
					if (!updateUser.UpdateUsername(username))
					{
						processor.WriteJson(new UsersResponse("Action 'update' failed to change username", null));
						return;
					}
				}

				// Change password
				if (password != null)
				{
					if (!updateUser.UpdatePassword(password))
					{
						processor.WriteJson(new UsersResponse("Action 'update' failed to change password", null));
						return;
					}
				}

				// Change role
				if (uri.Parameters.ContainsKey("role") && (role != updateUser.Role))
				{
					if (!updateUser.UpdateRole(role))
					{
						processor.WriteJson(new UsersResponse("Action 'update' failed to change role", null));
						return;
					}
				}

				// Return updated user
				logger.IfInfo(String.Format("Successfully updated user [id: {0}, username: {1}]", updateUser.UserId, updateUser.UserName));
				listOfUsers.Add(updateUser);

				processor.WriteJson(new UsersResponse(null, listOfUsers));
				return;
			}

			// Invalid action
			processor.WriteJson(new UsersResponse("Invalid action specified", null));
			return;
		}
	}
}
