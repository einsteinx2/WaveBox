using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WaveBox.DataModel.Model;
using System.IO;

namespace WaveBox.DataModel.Singletons
{
	public class Settings
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static string settingsFileName = "wavebox.conf";
		public static string SettingsTemplatePath() { return "res" + Path.DirectorySeparatorChar + settingsFileName; }
		public static string SettingsPath() { return WaveBoxMain.RootPath() + settingsFileName; }

		public static double Version { get { return 1.0; } }

		private static SettingsData settingsModel = new SettingsData();
		public static SettingsData SettingsModel { get { return settingsModel; } }

		public static Formatting JsonFormatting { get { return settingsModel.PrettyJson ? Formatting.Indented : Formatting.None; } }

		public static short Port { get { return settingsModel.Port; } }

		public static short MpdPort { get { return settingsModel.MpdPort; } }

		public static bool CrashReportEnable { get { return settingsModel.CrashReportEnable; } }

		public static bool NatEnable { get { return settingsModel.NatEnable; } }

		public static string PodcastFolder { get { return settingsModel.PodcastFolder; } }

		public static int PodcastCheckInterval { get { return settingsModel.PodcastCheckInterval; } }

		private static List<Folder> mediaFolders;
		public static List<Folder> MediaFolders { get { return mediaFolders; } }

		public static List<string> FolderArtNames { get { return settingsModel.FolderArtNames; } }


		public static void Reload()
		{
			ParseSettings();
		}

		private static void ParseSettings()
		{
			if (logger.IsInfoEnabled) logger.Info("Reading settings: " + SettingsPath());

			StreamReader reader = new StreamReader(SettingsPath());
			string configFile = RemoveJsonComments(reader);

			// Grab all settings from the file
			settingsModel = JsonConvert.DeserializeObject<SettingsData>(configFile);

			// Generate Folder objects from the media folders
			mediaFolders = PopulateMediaFolders();

			dynamic json = JsonConvert.DeserializeObject(configFile);
			bool settingsChanged = false;
			
			try
			{
				string podcastFolderTemp = json.podcastFolderDoesntExist;
				settingsModel.PodcastFolder = podcastFolderTemp;
				settingsChanged = true;
			}
			catch { }

			if (logger.IsInfoEnabled) logger.Info("settings changed: " + settingsChanged);
		}

		public static bool WriteSettings(string jsonString)
		{
			dynamic json = JsonConvert.DeserializeObject(jsonString);

			bool settingsChanged = false;

			try
			{
				short? port = json.port;
				if (port != null)
				{
					settingsModel.Port = (short)port;
					settingsChanged = true;
					if (logger.IsInfoEnabled) logger.Info("Setting 'port': " + settingsModel.Port);
				}
			}
			catch { }

			try
			{
				short? mpdPort = json.mpdPort;
				if (mpdPort != null)
				{
					settingsModel.MpdPort = (short)mpdPort;
					settingsChanged = true;
					if (logger.IsInfoEnabled) logger.Info("Setting 'mpdPort': " + settingsModel.MpdPort);
				}
			}
			catch { }

			try
			{
				bool? crashReportEnable = json.crashReportEnable;
				if (crashReportEnable != null)
				{
					settingsModel.CrashReportEnable = (bool)crashReportEnable;
					settingsChanged = true;
					if (logger.IsInfoEnabled) logger.Info("Setting 'crashReportEnable': " + settingsModel.CrashReportEnable);
				}
			}
			catch { }

			try
			{
				bool? natEnable = json.natEnable;
				if (natEnable != null)
				{
					settingsModel.NatEnable = (bool)natEnable;
					settingsChanged = true;
					if (logger.IsInfoEnabled) logger.Info("Setting 'natEnable': " + settingsModel.NatEnable);
				}
			}
			catch { }

			try
			{
				if (json.mediaFolders != null)
				{
					List<string> mediaFoldersTemp = new List<string>();
					if (logger.IsInfoEnabled) logger.Info("Setting 'mediaFolders':");
					foreach (string mediaFolderString in json.mediaFolders)
					{
						mediaFoldersTemp.Add(mediaFolderString);
						if (logger.IsInfoEnabled) logger.Info("\t" + mediaFolderString);
					}
					settingsModel.MediaFolders = mediaFoldersTemp;
					mediaFolders = PopulateMediaFolders();
					settingsChanged = true;
				}
			}
			catch { }

			try
			{
				string podcastFolderTemp = json.podcastFolder;
				if (podcastFolderTemp != null)
				{
					settingsModel.PodcastFolder = podcastFolderTemp;
					settingsChanged = true;
					if (logger.IsInfoEnabled) logger.Info("Setting 'podcastFolder': " + settingsModel.PodcastFolder);
				}
			}
			catch { }

			try
			{
				bool? prettyJsonTemp = json.prettyJson;
				if (prettyJsonTemp != null)
				{
					settingsModel.PrettyJson = (bool)prettyJsonTemp;
					settingsChanged = true;
					if (logger.IsInfoEnabled) logger.Info("Setting 'prettyJson': " + settingsModel.PrettyJson);
				}
			}
			catch { }

			try
			{
				int? podcastCheckIntervalTemp = json.podcastCheckInterval;
				if (podcastCheckIntervalTemp != null)
				{
					settingsModel.PodcastCheckInterval = (int)podcastCheckIntervalTemp;
					settingsChanged = true;
					if (logger.IsInfoEnabled) logger.Info("Setting 'podcastCheckInterval': " + settingsModel.PodcastCheckInterval);
				}
			}
			catch { }

			try
			{
				if (json.folderArtNames != null)
				{
					List<string> folderArtNamesTemp = new List<string>();
					if (logger.IsInfoEnabled) logger.Info("Setting 'folderArtNames':");
					foreach (string artName in json.folderArtNames)
					{
						folderArtNamesTemp.Add(artName);
						if (logger.IsInfoEnabled) logger.Info("\t" + artName);
					}
					settingsModel.FolderArtNames = folderArtNamesTemp;
					settingsChanged = true;
				}
			}
			catch { }

			// Now write the settings to disk
			if (settingsChanged)
			{
				FlushSettings();
			}

			return settingsChanged;
		}

		public static void FlushSettings()
		{
			// Write the settings data model to disk
			StreamWriter settingsOut = new StreamWriter(SettingsPath());
			settingsOut.Write(JsonConvert.SerializeObject(settingsModel, Formatting.Indented));
			settingsOut.Close();
		}

		public static void SettingsSetup()
		{
			if (!File.Exists(SettingsPath()))
			{
				try
				{
					if (logger.IsInfoEnabled) logger.Info("Setting file doesn't exist; Creating it. (WaveBox.conf)");
					StreamReader settingsTemplate = new StreamReader(SettingsTemplatePath());
					StreamWriter settingsOut = new StreamWriter(SettingsPath());

					settingsOut.Write(settingsTemplate.ReadToEnd());

					settingsTemplate.Close();
					settingsOut.Close();
				} 
				catch (Exception e)
				{
					logger.Error(e);
				}
			}

			Reload();
		}

		private static List<Folder> PopulateMediaFolders()
		{
			List<Folder> folders = new List<Folder>();

			foreach (string mediaFolderString in settingsModel.MediaFolders)
			{
				if (Directory.Exists(mediaFolderString))
				{
					Folder mediaFolder = new Folder(mediaFolderString, true);
					if (mediaFolder.FolderId == null)
					{
						mediaFolder.InsertFolder(true);
					}
					folders.Add(mediaFolder);
				}
				else 
				{
					if (logger.IsInfoEnabled) logger.Info("Media folder does not exist: " + mediaFolderString);
				}
			}

			return folders;
		}

		private static string RemoveJsonComments(StreamReader reader)
		{
			StringBuilder js = new StringBuilder();
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
				for (int i = 0; i < line.Length; i++)
				{
					curr = line[i];

					try
					{
						next = line[i + 1];
					}
					catch
					{
						//curr == '"' || curr == ','
						if (line.Length == 1 || !inBlockComment)
						{
							AppendDiscardingWhitespace(curr);
						}
						break;
					}

					if (!inBlockComment)
					{
						if (!inStringLiteral)
						{
							if (curr == '"')
							{
								inStringLiteral = true;
							}
							else if (curr == '/')
							{
								// this is a line comment. throw out the rest of the line.
								if (next == '/')
								{
									break;
								}
								// this is a block comment.  flip the block comment switch and continue to the next char
								if (next == '*')
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
							if (curr == '"')
							{
								inStringLiteral = false;
							}
							js.Append(curr);
							continue;
						}
					}
					else 
					{
						// if we are in a block comment, make sure that we shouldn't be ending the block comment
						if (curr == '*' && next == '/')
						{
							// advance the read position so we don't write the /
							i++;
							inBlockComment = false;
							continue;
						}
					}
				}
			}
			return js.ToString();
		}
	}

	public class SettingsData
	{
		[JsonProperty("port")]
		public short Port { get; set; }
		
		[JsonProperty("mpdPort")]
		public short MpdPort { get; set; }

		[JsonProperty("crashReportEnable")]
		public bool CrashReportEnable { get; set; }

		[JsonProperty("natEnable")]
		public bool NatEnable { get; set; }

		[JsonProperty("mediaFolders")]
		public List<string> MediaFolders { get; set; }
		
		[JsonProperty("podcastFolder")]
		public string PodcastFolder { get; set; }
		
		[JsonProperty("prettyJson")]
		public bool PrettyJson { get; set; }
		
		[JsonProperty("podcastCheckInterval")]
		public int PodcastCheckInterval { get; set; }
		
		[JsonProperty("folderArtNames")]
		public List<string> FolderArtNames { get; set; }
	}
}
