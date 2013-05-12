using System;
using System.Collections.Generic;
using WaveBox.Singletons;
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

