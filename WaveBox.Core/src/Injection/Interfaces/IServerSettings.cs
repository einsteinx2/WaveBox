using System;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core {
    public interface IServerSettings {
        string SettingsTemplatePath();
        string SettingsPath();

        ServerSettingsData SettingsModel { get; }

        bool PrettyJson { get; }

        short Port { get; }

        string Theme { get; }

        IList<string> MediaFolders { get; }

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

