using Microsoft.AspNet.SignalR;
using Owin;
using System;
using Microsoft.Owin.Diagnostics;

namespace WaveBox.DeviceSync
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

