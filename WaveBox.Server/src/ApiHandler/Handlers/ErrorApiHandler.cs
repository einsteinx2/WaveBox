﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core.Injected;
using WaveBox.Static;
using WaveBox.Service.Services.Http;

namespace WaveBox.ApiHandler.Handlers
{
	class ErrorApiHandler : IApiHandler
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private IHttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private string Err { get; set; }

		/// <summary>
		/// Constructor for ErrorApiHandler class
		/// </summary>
		public ErrorApiHandler(UriWrapper uri, IHttpProcessor processor)
		{
			Processor = processor;
			Uri = uri;
			Err = "Invalid API call";
		}

		/// <summary>
		/// Overload constructor for ErrorApiHandler class (custom error message)
		/// </summary>
		public ErrorApiHandler(UriWrapper uri, IHttpProcessor processor, string err)
		{
			Processor = processor;
			Uri = uri;
			Err = err;
		}

		/// <summary>
		/// Process logs the error, creates a JSON response, and sends
		/// it back to the user on bad API call
		/// </summary>
		public void Process()
		{
			if (logger.IsInfoEnabled) logger.Info(Err);

			Dictionary<string, string> response = new Dictionary<string, string>();
			response["error"] = Err;

			string json = JsonConvert.SerializeObject(response, Injection.Kernel.Get<IServerSettings>().JsonFormatting);
			Processor.WriteJson(json);
		}
	}
}
