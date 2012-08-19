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

        private static string podcastFolder = null;
        public static string PodcastFolder { get { return podcastFolder; } }

		private static List<Folder> mediaFolders;
		public static List<Folder> MediaFolders { get { return mediaFolders; } }

		public static void Reload()
		{
			ParseSettings();
		}

		private static void ParseSettings()
		{
            StreamReader reader = new StreamReader("WaveBox.conf");
            string configFile = RemoveJsonComments(reader);

            dynamic json = JsonConvert.DeserializeObject(configFile);

            podcastFolder = json.podcastFolder;
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
                    Console.WriteLine("[SETTINGS(1)] " + e.ToString());
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
                    Console.WriteLine("[SETTINGS(2)] " + e.ToString());
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
            string configFile = RemoveJsonComments(reader);

			dynamic json = JsonConvert.DeserializeObject(configFile);

			Console.WriteLine(json.mediaFolders + "\r\n");

			for (int i = 0; i < json.mediaFolders.Count; i++)
			{
				mf = new Folder(json.mediaFolders[i].ToString(), true);
				if (mf.FolderId == null)
				{
					mf.InsertFolder(true);
				}
				folders.Add(mf);
			}

			return folders;
		}

        private static string RemoveJsonComments(StreamReader reader)
        {
            var js = new StringBuilder();
            string line = null;
            bool inBlockComment = false;
            bool inStringLiteral = false;

            Action<char> AppendDiscardingWhitespace = (c) => 
            {
                if(c != '\t' && c != ' ') js.Append(c);
            };

            while ((line = reader.ReadLine()) != null)
            {
                char curr, next;
                for(int i = 0; i < line.Length; i++)
                {
                    curr = line[i];

                    try
                    {
                        next = line[i + 1];
                    }
                    catch
                    {
                        if(line.Length == 1 || curr == '"' || curr == ',')
                            AppendDiscardingWhitespace(curr);
                        break;
                    }

                    if(!inBlockComment)
                    {
                        if(!inStringLiteral)
                        {
                            if(curr == '"')
                            {
                                inStringLiteral = true;
                            }

                            else if(curr == '/')
                            {
                                // this is a line comment.  throw out the rest of the line.
                                if(next == '/') break;

                                // this is a block comment.  flip the block comment switch and continue to the next char
                                if(next == '*')
                                {
                                    inBlockComment = true;
                                    continue;
                                }
                            }

                            // if the combination of this char and the next char doesn't make a comment token, append to the string and continue.
                            AppendDiscardingWhitespace(curr);
                            continue;
                        }

                        else
                        {
                            if(curr == '"')
                            {
                                inStringLiteral = false;
                            }
                            AppendDiscardingWhitespace(curr);
                            continue;
                        }
                    }

                    else 
                    {
                        // if we are in a block comment, make sure that we shouldn't be ending the block comment
                        if(curr == '*' && next == '/')
                        {
                            inBlockComment = false;
                            continue;
                        }
                    }
                }
            }
            return js.ToString();
        }
	}
}
