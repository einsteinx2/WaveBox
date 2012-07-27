using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.HttpServer;

namespace WaveBox.ApiHandler.Handlers
{
	class ErrorApiHandler : IApiHandler
	{
		private HttpProcessor Processor { get; set; }
		private UriWrapper Uri { get; set; }
		private string Err { get; set; }

		public ErrorApiHandler(UriWrapper uri, HttpProcessor processor)
		{
			Processor = processor;
			Uri = uri;
			Err = "Invalid API call";
		}

		public ErrorApiHandler(UriWrapper uri, HttpProcessor processor, string err)
		{
			Processor = processor;
			Uri = uri;
			Err = err;
		}

		public void Process()
		{
			Console.WriteLine("[ERROR HANDLER]: " + Err);
			Processor.OutputStream.Write(Err);
		}
	}
}
