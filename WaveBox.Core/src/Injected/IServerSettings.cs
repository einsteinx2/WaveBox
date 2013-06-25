using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WaveBox.Model;

namespace WaveBox.Core.Injected
{
	public interface IServerSettings
	{
		string SettingsTemplatePath();
		string SettingsPath();

		ServerSettingsData SettingsModel { get; }

		Formatting JsonFormatting { get; }

		short Port { get; }

		short WsPort { get; }

		bool CrashReportEnable { get; }

		bool NatEnable { get; }

		string PodcastFolder { get; }

		int PodcastCheckInterval { get; }

		int SessionTimeout { get; }

		List<Folder> MediaFolders { get; }

		List<string> FolderArtNames { get; }


		void Reload();

		bool WriteSettings(string jsonString);

		void FlushSettings();

		void SettingsSetup();
	}
}

