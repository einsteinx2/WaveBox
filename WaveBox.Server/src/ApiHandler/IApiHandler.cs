using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;

namespace WaveBox.ApiHandler
{
	// This interface is implemented by all API handlers, to ensure that the Process() method is implemented
	public interface IApiHandler
	{
		void Process(UriWrapper uri, IHttpProcessor processor, User user);
	}
}
