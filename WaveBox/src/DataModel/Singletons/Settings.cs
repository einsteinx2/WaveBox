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
		public static string SETTINGS_PATH = "res" + Path.DirectorySeparatorChar + "WaveBox.conf";

		private static double version = 1.0;
		public static double Version { get { return version; } }

		private static List<Folder> mediaFolders;
		public static List<Folder> MediaFolders { get { return mediaFolders; } }

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
            if (!File.Exists("WaveBox.conf"))
            {
                try
                {
                    Console.WriteLine("[SETTINGS] " + "Setting file doesn't exist; Creating it. (WaveBox.conf)");
                    var settingsTemplate = new StreamReader("res" + Path.DirectorySeparatorChar + "WaveBox.conf");
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

            if (!File.Exists("wavebox.db"))
            {
                try
                {
                    Console.WriteLine("[SETTINGS] " + "Database file doesn't exist; Creating it. (wavebox.db)");

                    // new filestream on the template
                    var dbTemplate = new FileStream("res" + Path.DirectorySeparatorChar + "wavebox.db", FileMode.Open);

                    // a new byte array
                    byte[] dbData = new byte[dbTemplate.Length];

                    // read the template file into memory
                    dbTemplate.Read(dbData, 0, Convert.ToInt32(dbTemplate.Length));

                    // write it all out
                    System.IO.File.WriteAllBytes("wavebox.db", dbData);

                    // close the template file
                    dbTemplate.Close();
                } 
				catch (Exception e)
                {
                    Console.WriteLine("[SETTINGS] " + e.ToString());
                }
            }

            if (!Directory.Exists("art"))
            {
                Directory.CreateDirectory("art");
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
				if (mf.FolderId == null)
				{
					mf.AddToDatabase(true);
				}
				folders.Add(mf);
			}

			return folders;
		}
		
	}
}
