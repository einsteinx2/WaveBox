using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pms.DataModel.Model;
using Newtonsoft.Json;
using System.IO;

namespace pms.DataModel.Singletons
{
	public class Settings
	{
		private static Settings instance;
		public static string SETTINGS_PATH = "res/pms.conf";
		private static List<Folder> _mediaFolders;
		public static List<Folder> mediaFolders
		{
			get
			{
				return _mediaFolders;
			}
		}

		private Settings()
		{
			_settingsSetup();
		}

		public static Settings Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new Settings();
				}

				return instance;
			}
		}

		public static void reload()
		{
			_parseSettings();
		}

		private static void _parseSettings()
		{
			
		}

		private static void _settingsSetup()
		{
			if(!File.Exists("pms.conf"))
			{
				try
				{
					Console.WriteLine("Setting file doesn't exist; Creating it. (pms.conf)");
					var settingsTemplate = new StreamReader("res/pms.conf");
					var settingsOut = new StreamWriter("pms.conf");

					settingsOut.Write(settingsTemplate.ReadToEnd());

					settingsTemplate.Close();
					settingsOut.Close();
				}

				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}
		}

		public List<Folder> _populateMediaFolders()
		{
			StreamReader reader = new StreamReader("pms.conf");
			string omg = reader.ReadToEnd();

			string prop, value;

			dynamic asdf = JsonConvert.DeserializeObject(omg);

			Console.WriteLine(asdf.mediaFolders);

			

			
			return new List<Folder>();
		}
		
	}
}
