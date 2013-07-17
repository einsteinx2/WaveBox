using System;
using System.ServiceProcess;
using System.Configuration.Install;
using System.ComponentModel;

namespace WaveBox
{
	[RunInstaller(true)]
	public partial class WaveBoxServiceInstaller : Installer
	{
		private ServiceInstaller servInstaller;
		private ServiceProcessInstaller serviceProcessInstaller;

		public WaveBoxServiceInstaller()
		{
			this.servInstaller = new ServiceInstaller();
			this.serviceProcessInstaller = new ServiceProcessInstaller();

			// Service Installer
			this.servInstaller.DisplayName = "WaveBox Media Server";
			this.servInstaller.ServiceName = "WaveBox";
			this.servInstaller.StartType = ServiceStartMode.Automatic;
			this.servInstaller.Description = "The WaveBox media server service, this runs in the background to allow clients to communicate with this computer.";

			// Process Installer 
			this.serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
			this.serviceProcessInstaller.Password = null;
			this.serviceProcessInstaller.Username = null;

			// Project Installer
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {this.servInstaller,this.serviceProcessInstaller});
		}
	}
}

