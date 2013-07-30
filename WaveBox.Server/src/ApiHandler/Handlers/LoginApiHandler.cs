using System;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	public class LoginApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "login"; } set { } }

		/// <summary>
		/// Process returns a new session key for this user upon successful login
		/// <summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			try
			{
				logger.Info(String.Format("Authenticated user, generated new session: [user: {0}, key: {1}]", user.UserName, user.SessionId));

				string json = JsonConvert.SerializeObject(new LoginResponse(null, user.SessionId), Injection.Kernel.Get<IServerSettings>().JsonFormatting);
				processor.WriteJson(json);
			}
			catch (Exception e)
			{
				logger.Error(e.ToString());
			}
		}
	}
}
