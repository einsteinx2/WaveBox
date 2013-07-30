using System;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	public class LogoutApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "logout"; } set { } }

		/// <summary>
		/// Process logs this user out and destroys their current session
		/// <summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			try
			{
				// Destroy session
				logger.Info(String.Format("Logged out user, destroyed session: [user: {0}, key: {1}]", user.UserName, user.SessionId));
				user.DeleteSession(user.SessionId);

				string json = JsonConvert.SerializeObject(new LogoutResponse(null, user.SessionId), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e.ToString());
			}
		}
	}
}
