using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WaveBox.Core.Model;

namespace WaveBox.Core
{
	public interface IServerSettings
	{
		string SettingsTemplatePath();
		string SettingsPath();

		ServerSettingsData SettingsModel { get; }

		Formatting JsonFormatting { get; }

		short Port { get; }

		short WsPort { get; }

		string Theme { get; }

		IList<Folder> MediaFolders { get; }

		string PodcastFolder { get; }

		int PodcastCheckInterval { get; }

		int SessionTimeout { get; }

		IList<string> FolderArtNames { get; }

		bool CrashReportEnable { get; }

		IList<string> Services { get; }

		void Reload();

		bool WriteSettings(string jsonString);

		void FlushSettings();

		void SettingsSetup();
	}
}

