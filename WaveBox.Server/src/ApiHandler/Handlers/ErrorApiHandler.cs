using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers
{
	class ErrorApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public string Name { get { return "error"; } set { } }

		/// <summary>
		/// Overload for IApiHandler interface
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user)
		{
			this.Process(uri, processor, user, "Invalid API call");
		}

		/// <summary>
		/// Process logs the error, creates a JSON response, and send it back to the user on bad API call
		/// </summary>
		public void Process(UriWrapper uri, IHttpProcessor processor, User user, string error)
		{
			logger.IfInfo(error);

			ErrorResponse response = new ErrorResponse(error);
			string json = JsonConvert.SerializeObject(response, Injection.Kernel.Get<IServerSettings>().JsonFormatting);
			processor.WriteJson(json);
		}
	}
}
