﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Server;
using WaveBox.Static;

namespace WaveBox.ApiHandler
{
	public class ApiAuthenticate : IApiAuthenticate
	{
		/// <summary>
		/// Attempt to authenticate a user using a session ID
		/// </summary>
		public User AuthenticateSession(string session)
		{
			string username = Injection.Kernel.Get<IUserRepository>().UserNameForSessionid(session);
			if (username != null)
			{
				User user = Injection.Kernel.Get<IUserRepository>().UserForName(username);
				if (user != null)
				{
					// Update this user's session and return
					user.UpdateSession(session);
					return user;
				}
			}

			// Return null on failure or no match
			return null;
		}

		/// <summary>
		/// Attempt to authenticate a user using URI parameters
		/// </summary>
		public User AuthenticateUri(UriWrapper uri)
		{
			// Attempt to parse various parameters from the URI
			string sessionId = null;
			string username = null;
			string password = null;
			string clientName = null;

			try
			{
				uri.Parameters.TryGetValue("s", out sessionId);
				uri.Parameters.TryGetValue("u", out username);
				uri.Parameters.TryGetValue("p", out password);
				uri.Parameters.TryGetValue("c", out clientName);
			}
			catch
			{
			}

			// If logging in, we are creating a new session
			if (uri.ApiAction == "login")
			{
				// Must use username and password, and create a session
				User user = Injection.Kernel.Get<IUserRepository>().UserForName(username);

				// Validate User ID, and ensure successful session creation
				if (user.UserId != null && user.CreateSession(password, clientName))
				{
					return user;
				}
			}

			// Otherwise, check for a session key parameter
			if (sessionId != null)
			{
				// This will return the user on success, or null on failure
				return this.AuthenticateSession(sessionId);
			}

			// On failure or no session, return null
			return null;
		}
	}
}
