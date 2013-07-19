using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WaveBox.Model;

namespace WaveBox.Core.Injection
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

		List<Folder> MediaFolders { get; }

		string PodcastFolder { get; }

		int PodcastCheckInterval { get; }

		int SessionTimeout { get; }

		List<string> FolderArtNames { get; }

		bool CrashReportEnable { get; }

		List<string> Services { get; }

		void Reload();

		bool WriteSettings(string jsonString);

		void FlushSettings();

		void SettingsSetup();
	}
}

