using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using System.IO;

namespace WaveBox.DataModel.Singletons
{
	public class Settings
	{
		public static string SETTINGS_PATH = "res/WaveBox.conf";

		private static double version = 1.0;
		public static double Version
		{
			get
			{
				return version;
			}
		}

		private static List<Folder> mediaFolders;
		public static List<Folder> MediaFolders
		{
			get
			{
				return mediaFolders;
			}
		}

		public static void Reload()
		{
			ParseSettings();
		}

		private static void ParseSettings()
		{
			mediaFolders = PopulateMediaFolders();
		}

		public static void SettingsSetup()
		{
			if(!File.Exists("WaveBox.conf"))
			{
				try
				{
					Console.WriteLine("[SETTINGS] " + "Setting file doesn't exist; Creating it. (WaveBox.conf)");
					var settingsTemplate = new StreamReader("res/WaveBox.conf");
					var settingsOut = new StreamWriter("WaveBox.conf");

					settingsOut.Write(settingsTemplate.ReadToEnd());

					settingsTemplate.Close();
					settingsOut.Close();
				}

				catch (Exception e)
				{
					Console.WriteLine("[SETTINGS] " + e.ToString());
				}
			}

			Reload();
		}

		private static List<Folder> PopulateMediaFolders()
		{
			var folders = new List<Folder>();
			Folder mf = null;
			StreamReader reader = new StreamReader("WaveBox.conf");
			string configFile = reader.ReadToEnd();

			dynamic json = JsonConvert.DeserializeObject(configFile);

			Console.WriteLine(json.mediaFolders + "\r\n");

			for (int i = 0; i < json.mediaFolders.Count; i++)
			{
				mf = new Folder(json.mediaFolders[i].ToString(), true);
				if (mf.FolderId == 0)
				{
					mf.AddToDatabase(true);
				}
				folders.Add(mf);
			}

			return folders;
		}
		
	}
}
