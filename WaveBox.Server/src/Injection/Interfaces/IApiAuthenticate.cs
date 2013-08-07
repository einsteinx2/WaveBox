using System;
using WaveBox.ApiHandler;
using WaveBox.Core.Model;

namespace WaveBox.Server
{
	public interface IApiAuthenticate
	{
		User AuthenticateSession(string session);

		User AuthenticateUri(UriWrapper uri);
	}
}
