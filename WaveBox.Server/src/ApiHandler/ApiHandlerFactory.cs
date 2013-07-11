using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Ninject;
using WaveBox.ApiHandler.Handlers;
using WaveBox.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;
using WaveBox.Core.Extensions;
using WaveBox.Core.Injected;

namespace WaveBox.ApiHandler
{
	public static class ApiHandlerFactory
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static List<IApiHandler> handlers = new List<IApiHandler>();

		/// <summary>
		/// Create an API Handler based upon source URI, or serve web interface if no API call present
		/// </summary>
		public static IApiHandler CreateApiHandler(string uri, HttpProcessor processor)
		{
			if (logger.IsInfoEnabled) logger.Info("uri: " + uri);

			// Turn the input string into a UriWrapper, so we can parse its components with ease
			UriWrapper uriW = new UriWrapper(uri);

			// Create a blank user for sanity
			User user = null;

			// Ensure URL contains API call
			if (!uriW.IsApiCall)
			{
				// Else, no API call present, so serve the web interface
				WebApiHandler instance = (WebApiHandler)handlers.Single(x => x.Name == "web");
				instance.Prepare(uriW, processor, user);
				return instance;
			}

			// Grab the action and authentication info (if call is /api/, no action, so return error)
			string action = null;
			try
			{
				action = uriW.Action;
			}
			catch
			{
				// Send back error API handler
				ErrorApiHandler instance = (ErrorApiHandler)handlers.Single(x => x.Name == "error");
				instance.Prepare(uriW, processor, user, "No API call action specified");
				return instance;
			}

			string sessionId = null;
			string username = null;
			string password = null;
			string clientName = null;

			try
			{
				uriW.Parameters.TryGetValue("s", out sessionId);
				uriW.Parameters.TryGetValue("u", out username);
				uriW.Parameters.TryGetValue("p", out password);
				uriW.Parameters.TryGetValue("c", out clientName);
			}
			catch
			{
				// If we fail to grab a parameter needed by a requested handler, it will report the error
			}

			// Attempt to parse session ID from cookie if not already set
			if (sessionId == null)
			{
				if (processor.HttpHeaders.ContainsKey("Cookie"))
				{
					// Split each cookie into pairs
					string[] cookies = processor.HttpHeaders["Cookie"].ToString().Split(new [] {';', ',', '='}, StringSplitOptions.RemoveEmptyEntries);

					// Iterate all cookies
					for (int i = 0; i < cookies.Length; i += 2)
					{
						// Look for wavebox_session cookie
						if (cookies[i] == "wavebox_session")
						{
							sessionId = cookies[i + 1];
							logger.Info("Cookie wavebox_session: " + sessionId);
							break;
						}
					}
				}
			}

			// Authenticate user
			user = Authenticate(action, sessionId, username, password, clientName);
			if (user == null)
			{
				// No user object returned, so we failed to authenticate
				ErrorApiHandler instance = (ErrorApiHandler)handlers.Single(x => x.Name == "error");
				instance.Prepare(uriW, processor, user, "Authentication failed");
				return instance;
			}

			// user.SessionId will be generated on new login, so that takes precedence
			sessionId = user.SessionId == null ? sessionId : user.SessionId;
			if (sessionId != null)
			{
				// Calculate session timeout time (DateTime.Now UTC + Injection.Kernel.Get<IServerSettings>().SessionTimeout minutes)
				DateTime expire = DateTime.Now.ToUniversalTime().AddMinutes(Injection.Kernel.Get<IServerSettings>().SessionTimeout);

				// Add a delayed header so cookie will be reset on each API call (to prevent timeout)
				processor.DelayedHeaders["Set-Cookie"] = String.Format("wavebox_session={0}; Expires={1};", sessionId, expire.ToRFC1123());
			}

			// Check for invalid API call
			if (!handlers.Any(x => x.Name == action))
			{
				// No user object returned, so we failed to authenticate
				ErrorApiHandler instance = (ErrorApiHandler)handlers.Single(x => x.Name == "error");
				instance.Prepare(uriW, processor, user);
				return instance;
			}

			// Get API handler from factory which matches the given action name
			IApiHandler api = handlers.Single(x => x.Name == action);

			// Set parameters, send it back
			api.Prepare(uriW, processor, user);
			return api;
		}

		/// <summary>
		/// Initialize the factory by dynamically loading all API handlers which implement IApiHandler interface.
		/// </summary>
		public static void Initialize()
		{
			try
			{
				// Grab all available types which implement IApiHandler
				foreach (Type t in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IApiHandler))))
				{
					// Instantiate the instance to register it without further reflection
					IApiHandler instance = (IApiHandler)Activator.CreateInstance(t);

					// Register valid API handlers
					if (logger.IsInfoEnabled) logger.Info("Discovered API handler: " + instance.Name + " -> " + t);
					handlers.Add(instance);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
		}

		/// <summary>
		/// Return a list of currently registered API handlers
		/// <summary>
		public static List<string> GetApiHandlers()
		{
			return handlers.Select(x => x.Name).ToList();
		}

		/// <summary>
		/// Authenticate a user or session against the database
		/// </summary>
		public static User Authenticate(string action, string sessionId, string username, string password, string clientName)
		{
			User user = null;
			if (action == "login")
			{
				// Must use username and password, and create a session
				user = new User.Factory().CreateUser(username);
				if (user.UserId == null || !user.CreateSession(password, clientName))
				{
					user = null;
				}
			}
			else
			{
				// Must use sessionId
				username = User.UserNameForSessionid(sessionId);
				if ((object)username != null)
				{
					user = new User.Factory().CreateUser(username);

					// Update this user's session
					user.UpdateSession(sessionId);
				}
			}

			return user;
		}
	}
}
