using System;
using WaveBox.Model;
using WaveBox.Service.Services.Http;

namespace WaveBox.ApiHandler
{
	public interface IApiHandler
	{
		// Ensure all API handlers have a name
		string Name { get; set; }

		// Ensure all API handlers implement a preparation function, to set parameters from factory
		void Prepare(UriWrapper uriW, IHttpProcessor processor, User user);

		// Ensure all API handlers implement a processing function
		void Process();
	}
}
