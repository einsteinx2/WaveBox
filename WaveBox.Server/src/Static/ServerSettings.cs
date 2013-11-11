using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Service;
using WaveBox.Core.Static;

namespace WaveBox.Static
{
	public class ServerSettings : IServerSettings
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly string settingsFileName = "wavebox.conf";
		public string SettingsTemplatePath() { return ServerUtility.ExecutablePath() + "res" + Path.DirectorySeparatorChar + settingsFileName; }
		public string SettingsPath() { return ServerUtility.RootPath() + settingsFileName; }

		private ServerSettingsData settingsModel = new ServerSettingsData();
		public ServerSettingsData SettingsModel { get { return settingsModel; } }

		public Formatting JsonFormatting { get { return settingsModel.PrettyJson ? Formatting.Indented : Formatting.None; } }

		public short Port { get { return settingsModel.Port; } }

		public string Theme { get { return settingsModel.Theme; } }

		public IList<String> MediaFolders { get { return settingsModel.MediaFolders; } }

		public int SessionTimeout { get { return settingsModel.SessionTimeout; } }

		public IList<string> FolderArtNames { get { return settingsModel.FolderArtNames; } }

		public bool CrashReportEnable { get { return settingsModel.CrashReportEnable; } }

		public IList<string> Services { get { return settingsModel.Services; } }

		public void Reload()
		{
			ParseSettings();
		}

		private void ParseSettings()
		{
			logger.IfInfo("Reading settings: " + SettingsPath());

			string configFile = "";
			try
			{
				StreamReader reader = new StreamReader(SettingsPath());
				configFile = RemoveJsonComments(reader);
				reader.Close();
			}
			catch (Exception e)
			{
				logger.Error("Could not open configuration file: " + SettingsPath());
				logger.Error(e);
			}

			// Grab all settings from the file
			settingsModel = JsonConvert.DeserializeObject<ServerSettingsData>(configFile);

			// Generate Folder objects from the media folders
			PrepareMediaFolders();

			dynamic json = JsonConvert.DeserializeObject(configFile);
			bool settingsChanged = false;

			try
			{
				string podcastFolderTemp = json.podcastFolderDoesntExist;
				settingsModel.PodcastFolder = podcastFolderTemp;
				settingsChanged = true;
			}
			catch { }

			logger.IfInfo("settings changed: " + settingsChanged);
		}

		public bool WriteSettings(string jsonString)
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
					logger.IfInfo("Setting 'port': " + settingsModel.Port);
				}
			}
			catch { }

			try
			{
				string themeTemp = json.theme;
				if (themeTemp != null)
				{
					settingsModel.Theme = themeTemp;
					settingsChanged = true;
					logger.IfInfo("Setting 'theme': " + settingsModel.Theme);
				}
			}
			catch { }

			try
			{
				if (json.mediaFolders != null)
				{
					List<string> mediaFoldersTemp = new List<string>();
					logger.IfInfo("Setting 'mediaFolders':");
					foreach (string mediaFolderString in json.mediaFolders)
					{
						mediaFoldersTemp.Add(mediaFolderString);
						logger.IfInfo("\t" + mediaFolderString);
					}
					settingsModel.MediaFolders = mediaFoldersTemp;
					settingsChanged = true;
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
					logger.IfInfo("Setting 'prettyJson': " + settingsModel.PrettyJson);
				}
			}
			catch { }

			try
			{
				int? sessionTimeoutTemp = json.sessionTimeout;
				if (sessionTimeoutTemp != null)
				{
					settingsModel.SessionTimeout = (int)sessionTimeoutTemp;
					settingsChanged = true;
					logger.IfInfo("Setting 'sessionTimeout': " + settingsModel.SessionTimeout);
				}
			}
			catch { }

			try
			{
				if (json.folderArtNames != null)
				{
					List<string> folderArtNamesTemp = new List<string>();
					logger.IfInfo("Setting 'folderArtNames': ");
					foreach (string artName in json.folderArtNames)
					{
						folderArtNamesTemp.Add(artName);
						logger.IfInfo("\t" + artName);
					}
					settingsModel.FolderArtNames = folderArtNamesTemp;
					settingsChanged = true;
				}
			}
			catch { }

			// Advanced configuration

			try
			{
				bool? crashReportEnable = json.crashReportEnable;
				if (crashReportEnable != null)
				{
					settingsModel.CrashReportEnable = (bool)crashReportEnable;
					settingsChanged = true;
					logger.IfInfo("Setting 'crashReportEnable': " + settingsModel.CrashReportEnable);
				}
			}
			catch { }

			try
			{
				if (json.services != null)
				{
					List<string> servicesTemp = new List<string>();
					logger.IfInfo("Setting 'services':");
					foreach (string service in json.services)
					{
						servicesTemp.Add(service);
						logger.IfInfo("\t" + service);
					}
					settingsModel.Services = servicesTemp;
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

		public void FlushSettings()
		{
			// Read in the settings template, with placeholders
			string template = null;
			try
			{
				StreamReader templateIn = new StreamReader(SettingsTemplatePath() + ".template");
				template = templateIn.ReadToEnd();
				templateIn.Close();
			}
			catch (Exception e)
			{
				logger.Error(e);
			}

			// Begin template creation with an auto-generated line stating WaveBox version and date/time generated
			StringBuilder templateBuilder = new StringBuilder("// WaveBox auto-generated file on " + DateTime.UtcNow.ToString("MM/dd/yyyy, hh:mm:sstt") + "\n");

			// Add the template to templateBuilder
			templateBuilder.Append(template);

			// Check for any null strings
			if (settingsModel.PodcastFolder == null)
			{
				settingsModel.PodcastFolder = "";
			}

			// Replace all template placeholders with their actual values
			// Notes:
			//  - all settings must be converted to string
			//  - convert booleans using ToString().ToLower()
			//  - convert lists using ToCSV(true) extension method, for quoted list
			//  - ... sorry that this is probably the best way to do this.
			try
			{
				templateBuilder
					.Replace("{setting-port}", settingsModel.Port.ToString())
					.Replace("{setting-theme}", settingsModel.Theme)
					.Replace("{setting-mediaFolders}", settingsModel.MediaFolders.ToCSV(true))
					.Replace("{setting-sessionTimeout}", settingsModel.SessionTimeout.ToString())
					.Replace("{setting-prettyJson}", settingsModel.PrettyJson.ToString().ToLower())
					.Replace("{setting-folderArtNames}", settingsModel.FolderArtNames.ToCSV(true))
					// Advanced configuration
					.Replace("{setting-crashReportEnable}", settingsModel.CrashReportEnable.ToString().ToLower());

				// For services, only enable them if specified in JSON. Disable otherwise
				List<string> services = new List<string>{"nat", "nowplaying", "zeroconf"};
				foreach (string s in services)
				{
					if (settingsModel.Services.Contains(s))
					{
						templateBuilder.Replace("{setting-services-" + s + "}", s);
					}
					else
					{
						// If no match, disable this setting
						templateBuilder.Replace("{setting-services-" + s + "}", "!" + s);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}

			template = templateBuilder.ToString();

			// Write the settings data model to disk
			try
			{
				StreamWriter settingsOut = new StreamWriter(SettingsPath());
				settingsOut.Write(template);
				settingsOut.Close();
			}
			catch (Exception e)
			{
				logger.Error("Could not write settings to file: " + SettingsPath());
				logger.Error(e);
			}
		}

		public void SettingsSetup()
		{
			if (!File.Exists(SettingsPath()))
			{
				try
				{
					logger.IfInfo("Setting file doesn't exist; Creating it : " + settingsFileName);
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

		private void PrepareMediaFolders()
		{
			try
			{
				foreach (string mediaFolderString in settingsModel.MediaFolders)
				{
					if (Directory.Exists(mediaFolderString))
					{
						Folder mediaFolder = CreateFolder(mediaFolderString, true);
						if (mediaFolder.FolderId == null)
						{
							mediaFolder.InsertFolder(true);
						}
					}
					else
					{
						logger.IfInfo("Media folder does not exist: " + mediaFolderString);
					}
				}
			}
			catch
			{
				logger.Warn("No media folders specified in configuration file!");
			}
		}

		private Folder CreateFolder(string path, bool mediafolder)
		{
			if (path == null || path == "")
			{
				// No path so just return a folder
				return new Folder();
			}

			ISQLiteConnection conn = null;
			try
			{
				// Trim all trailing slashes from paths, to prevent potential constraint issues
				path = path.TrimEnd('/', '\\');

				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				IList<Folder> result = conn.Query<Folder>("SELECT * FROM Folder WHERE FolderPath = ? AND MediaFolderId IS NULL", path);

				foreach (Folder f in result)
				{
					if (path.Equals(f.FolderPath))
					{
						return f;
					}
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			// If not in database, return a folder object with the specified parameters
			Folder folder = new Folder();
			folder.FolderPath = path;
			folder.FolderName = Path.GetFileName(path);
			return folder;
		}

		private string RemoveJsonComments(StreamReader reader)
		{
			StringBuilder js = new StringBuilder();
			string line = null;
			bool inBlockComment = false;
			bool inStringLiteral = false;

			Action<char> AppendDiscardingWhitespace = (c) =>
			{
				if (c != '\t' && c != ' ')
				{
					js.Append(c);
				}
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
}
