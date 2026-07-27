using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cirrious.MvvmCross.Plugins.Sqlite;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Service;
using WaveBox.Core.Static;

namespace WaveBox.Static {
    public class ServerSettings : IServerSettings {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(ServerSettings));

        // wavebox.conf allows // and /* */ comments plus trailing commas; STJ handles both natively
        private static readonly WaveBoxJsonContext readContext = new WaveBoxJsonContext(new JsonSerializerOptions {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        private static readonly JsonDocumentOptions documentOptions = new JsonDocumentOptions {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly string settingsFileName = "wavebox.conf";
        public string SettingsTemplatePath() { return ServerUtility.ExecutablePath() + "res" + Path.DirectorySeparatorChar + settingsFileName; }
        public string SettingsPath() { return ServerUtility.RootPath() + settingsFileName; }

        private ServerSettingsData settingsModel = new ServerSettingsData();
        public ServerSettingsData SettingsModel { get { return settingsModel; } }

        public bool PrettyJson { get { return settingsModel.PrettyJson; } }

        public int Port { get { return settingsModel.Port; } }

        public string Theme { get { return settingsModel.Theme; } }

        public IList<String> MediaFolders { get { return settingsModel.MediaFolders; } }

        public int SessionTimeout { get { return settingsModel.SessionTimeout; } }

        public IList<string> FolderArtNames { get { return settingsModel.FolderArtNames; } }

        public bool CrashReportEnable { get { return settingsModel.CrashReportEnable; } }

        public IList<string> Services { get { return settingsModel.Services; } }

        public void Reload() {
            ParseSettings();
        }

        private void ParseSettings() {
            logger.IfInfo("Reading settings: " + SettingsPath());

            try {
                string configFile = File.ReadAllText(SettingsPath());
                settingsModel = JsonSerializer.Deserialize(configFile, readContext.ServerSettingsData) ?? new ServerSettingsData();
            } catch (Exception e) {
                logger.Error("Could not parse configuration file: " + SettingsPath());
                logger.Error(e);
                settingsModel = new ServerSettingsData();
            }

            // Generate Folder objects from the media folders
            PrepareMediaFolders();
        }

        public bool WriteSettings(string jsonString) {
            JsonNode json;
            try {
                json = JsonNode.Parse(jsonString, null, documentOptions);
            } catch (Exception e) {
                logger.Error("Could not parse settings update: " + e.Message);
                return false;
            }

            if (json == null) {
                return false;
            }

            bool settingsChanged = false;

            try {
                int? port = json["port"] != null ? json["port"].GetValue<int>() : (int?)null;
                if (port != null) {
                    settingsModel.Port = (int)port;
                    settingsChanged = true;
                    logger.IfInfo("Setting 'port': " + settingsModel.Port);
                }
            } catch { }

            try {
                string themeTemp = json["theme"] != null ? json["theme"].GetValue<string>() : null;
                if (themeTemp != null) {
                    settingsModel.Theme = themeTemp;
                    settingsChanged = true;
                    logger.IfInfo("Setting 'theme': " + settingsModel.Theme);
                }
            } catch { }

            try {
                if (json["mediaFolders"] is JsonArray mediaFolders) {
                    List<string> mediaFoldersTemp = new List<string>();
                    logger.IfInfo("Setting 'mediaFolders':");
                    foreach (JsonNode mediaFolderNode in mediaFolders) {
                        string mediaFolderString = mediaFolderNode.GetValue<string>();
                        mediaFoldersTemp.Add(mediaFolderString);
                        logger.IfInfo("\t" + mediaFolderString);
                    }
                    settingsModel.MediaFolders = mediaFoldersTemp;
                    settingsChanged = true;
                }
            } catch { }

            try {
                bool? prettyJsonTemp = json["prettyJson"] != null ? json["prettyJson"].GetValue<bool>() : (bool?)null;
                if (prettyJsonTemp != null) {
                    settingsModel.PrettyJson = (bool)prettyJsonTemp;
                    settingsChanged = true;
                    logger.IfInfo("Setting 'prettyJson': " + settingsModel.PrettyJson);
                }
            } catch { }

            try {
                int? sessionTimeoutTemp = json["sessionTimeout"] != null ? json["sessionTimeout"].GetValue<int>() : (int?)null;
                if (sessionTimeoutTemp != null) {
                    settingsModel.SessionTimeout = (int)sessionTimeoutTemp;
                    settingsChanged = true;
                    logger.IfInfo("Setting 'sessionTimeout': " + settingsModel.SessionTimeout);
                }
            } catch { }

            try {
                if (json["folderArtNames"] is JsonArray folderArtNames) {
                    List<string> folderArtNamesTemp = new List<string>();
                    logger.IfInfo("Setting 'folderArtNames': ");
                    foreach (JsonNode artNameNode in folderArtNames) {
                        string artName = artNameNode.GetValue<string>();
                        folderArtNamesTemp.Add(artName);
                        logger.IfInfo("\t" + artName);
                    }
                    settingsModel.FolderArtNames = folderArtNamesTemp;
                    settingsChanged = true;
                }
            } catch { }

            // Advanced configuration

            try {
                bool? crashReportEnable = json["crashReportEnable"] != null ? json["crashReportEnable"].GetValue<bool>() : (bool?)null;
                if (crashReportEnable != null) {
                    settingsModel.CrashReportEnable = (bool)crashReportEnable;
                    settingsChanged = true;
                    logger.IfInfo("Setting 'crashReportEnable': " + settingsModel.CrashReportEnable);
                }
            } catch { }

            try {
                if (json["services"] is JsonArray servicesArray) {
                    List<string> servicesTemp = new List<string>();
                    logger.IfInfo("Setting 'services':");
                    foreach (JsonNode serviceNode in servicesArray) {
                        string service = serviceNode.GetValue<string>();
                        servicesTemp.Add(service);
                        logger.IfInfo("\t" + service);
                    }
                    settingsModel.Services = servicesTemp;
                    settingsChanged = true;
                }
            } catch { }

            // Now write the settings to disk
            if (settingsChanged) {
                FlushSettings();
            }

            return settingsChanged;
        }

        public void FlushSettings() {
            // Read in the settings template, with placeholders
            string template = null;
            try {
                StreamReader templateIn = new StreamReader(SettingsTemplatePath() + ".template");
                template = templateIn.ReadToEnd();
                templateIn.Close();
            } catch (Exception e) {
                logger.Error(e);
            }

            // Begin template creation with an auto-generated line stating WaveBox version and date/time generated
            StringBuilder templateBuilder = new StringBuilder("// WaveBox auto-generated file on " + DateTime.UtcNow.ToString("MM/dd/yyyy, hh:mm:sstt") + "\n");

            // Add the template to templateBuilder
            templateBuilder.Append(template);

            // Check for any null strings
            if (settingsModel.PodcastFolder == null) {
                settingsModel.PodcastFolder = "";
            }

            // Replace all template placeholders with their actual values
            // Notes:
            //  - all settings must be converted to string
            //  - convert booleans using ToString().ToLower()
            //  - convert lists using ToQuotedJsonCsv, so items re-parse as valid JSON strings
            //  - ... sorry that this is probably the best way to do this.
            try {
                templateBuilder
                .Replace("{setting-port}", settingsModel.Port.ToString())
                .Replace("{setting-theme}", settingsModel.Theme)
                .Replace("{setting-mediaFolders}", ToQuotedJsonCsv(settingsModel.MediaFolders))
                .Replace("{setting-sessionTimeout}", settingsModel.SessionTimeout.ToString())
                .Replace("{setting-prettyJson}", settingsModel.PrettyJson.ToString().ToLower())
                .Replace("{setting-folderArtNames}", ToQuotedJsonCsv(settingsModel.FolderArtNames))
                // Advanced configuration
                .Replace("{setting-crashReportEnable}", settingsModel.CrashReportEnable.ToString().ToLower());

                // For services, only enable them if specified in JSON. Disable otherwise
                List<string> services = new List<string> {"nat", "nowplaying", "zeroconf"};
                foreach (string s in services) {
                    if (settingsModel.Services.Contains(s)) {
                        templateBuilder.Replace("{setting-services-" + s + "}", s);
                    } else {
                        // If no match, disable this setting
                        templateBuilder.Replace("{setting-services-" + s + "}", "!" + s);
                    }
                }
            } catch (Exception e) {
                logger.Error(e);
            }

            template = templateBuilder.ToString();

            // Write the settings data model to disk
            try {
                StreamWriter settingsOut = new StreamWriter(SettingsPath());
                settingsOut.Write(template);
                settingsOut.Close();
            } catch (Exception e) {
                logger.Error("Could not write settings to file: " + SettingsPath());
                logger.Error(e);
            }
        }

        // Render a list as comma-delimited quoted JSON strings for the conf template. A plain quoted
        // CSV corrupts the conf on Windows: unescaped backslashes in paths are invalid JSON escapes,
        // so the whole file silently fails to parse on the next reload
        private static string ToQuotedJsonCsv(IList<string> list) {
            if (list == null || list.Count == 0) {
                return "\"\"";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < list.Count; i++) {
                if (i > 0) {
                    builder.Append(", ");
                }
                builder.Append('"').Append(JsonEncodedText.Encode(list[i]).ToString()).Append('"');
            }
            return builder.ToString();
        }

        public void SettingsSetup() {
            if (!File.Exists(SettingsPath())) {
                try {
                    logger.IfInfo("Setting file doesn't exist; Creating it : " + settingsFileName);
                    StreamReader settingsTemplate = new StreamReader(SettingsTemplatePath());
                    StreamWriter settingsOut = new StreamWriter(SettingsPath());

                    settingsOut.Write(settingsTemplate.ReadToEnd());

                    settingsTemplate.Close();
                    settingsOut.Close();
                } catch (Exception e) {
                    logger.Error(e);
                }
            }

            Reload();
        }

        private void PrepareMediaFolders() {
            try {
                foreach (string mediaFolderString in settingsModel.MediaFolders) {
                    if (Directory.Exists(mediaFolderString)) {
                        Folder mediaFolder = CreateFolder(mediaFolderString, true);
                        if (mediaFolder.FolderId == null) {
                            mediaFolder.InsertFolder(true);
                        }
                    } else {
                        logger.IfInfo("Media folder does not exist: " + mediaFolderString);
                    }
                }
            } catch {
                logger.Warn("No media folders specified in configuration file!");
            }
        }

        private static Folder CreateFolder(string path, bool mediafolder) {
            if (path == null || path == "") {
                // No path so just return a folder
                return new Folder();
            }

            ISQLiteConnection conn = null;
            try {
                // Trim all trailing slashes from paths, to prevent potential constraint issues
                path = path.TrimEnd('/', '\\');

                conn = Injection.Get<IDatabase>().GetSqliteConnection();
                IList<Folder> result = conn.Query<Folder>("SELECT * FROM Folder WHERE FolderPath = ? AND MediaFolderId IS NULL", path);

                foreach (Folder f in result) {
                    if (path.Equals(f.FolderPath)) {
                        return f;
                    }
                }
            } catch (Exception e) {
                logger.Error(e);
            } finally {
                Injection.Get<IDatabase>().CloseSqliteConnection(conn);
            }

            // If not in database, return a folder object with the specified parameters
            Folder folder = new Folder();
            folder.FolderPath = path;
            folder.FolderName = Path.GetFileName(path);
            return folder;
        }

    }
}
