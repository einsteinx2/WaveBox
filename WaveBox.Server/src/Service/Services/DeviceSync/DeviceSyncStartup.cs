using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Diagnostics;
using Owin;

namespace WaveBox.Service.Services.DeviceSync
{
	public class DeviceSyncStartup
	{
		public void Configuration(IAppBuilder app)
		{
			app.UseShowExceptions();
			app.MapConnection<RawConnection>("/raw", new ConnectionConfiguration { EnableCrossDomain = true });
			app.MapHubs(new HubConfiguration() { EnableCrossDomain = true, EnableDetailedErrors = true });
		}
	}
}

