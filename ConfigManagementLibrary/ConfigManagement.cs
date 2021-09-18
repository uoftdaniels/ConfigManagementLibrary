using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#if SSHARP
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharp;
#else
using System.IO;
using System.Reflection;
#endif

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Daniels.Config
{
    public static class ConfigManagement
    {
        public class Config
        {
            [JsonIgnore]
            private Dictionary<string, string> _defaultLookUpFoldersStorage;

            [JsonProperty("DefaultLookUpFolders")]
            internal Dictionary<string, string> DefaultLookUpFoldersStorage
            {
                private get { return _defaultLookUpFoldersStorage; }
                set
                {
                    _defaultLookUpFoldersStorage = value;
                    _defaultLookUpFolders = new ReadOnlyDictionary<string, string>(_defaultLookUpFoldersStorage);
                    _lookUpFolders = null;
                }
            }

            [JsonIgnore]
            private ReadOnlyDictionary<string, string> _defaultLookUpFolders;
            [JsonIgnore]
            public ReadOnlyDictionary<string, string> DefaultLookUpFolders { get { return _defaultLookUpFolders; } }

            [JsonIgnore]
            private string[] _lookUpFolders = null;
            [JsonIgnore]
            internal string[] LookUpFolders
            {
                get
                {
                    if (_lookUpFolders == null)
                    {
                        string[] folders = new string[_defaultLookUpFoldersStorage.Count];
                        //_defaultLookUpFoldersStorage.Values.CopyTo(folders, 0);

                        int i = 0;
                        foreach (var kv in _defaultLookUpFoldersStorage)
                        //for (int i = 0; i < folders.Length; i++)
                        {
                            switch (kv.Key)
                            {
                                case "<program>":
#if SSHARP
                                    folders[i] = InitialParametersClass.ProgramDirectory.ToString();
#else
                                    folders[i] = AppDomain.CurrentDomain.BaseDirectory;
#endif
                                    break;
                                case "<library>":
                                    string codeBase = Assembly.GetExecutingAssembly().GetName().CodeBase;
                                    Uri uri = new Uri(codeBase);
                                    string path = Uri.UnescapeDataString(uri.LocalPath);
                                    folders[i] = Path.GetDirectoryName(Uri.UnescapeDataString(uri.LocalPath));
                                    break;
                                default:
                                    folders[i] = kv.Value;
                                    break;
                            }
                            i++;
                        }

                        _lookUpFolders = folders;
                    }
                    return _lookUpFolders;
                }
            }

            [JsonProperty("DefaultConfigFileNamePattern")]
            private string _defaultConfigFileNamePattern = @"{0}-P{1:D2}";
            [JsonIgnore]
            public string DefaultConfigFileNamePattern { get { return _defaultConfigFileNamePattern; } private set { _defaultConfigFileNamePattern = value; } }

            [JsonProperty("DefaultConfigFileExtension")]
            private string _defaultConfigFileExtension = @".json";
            [JsonIgnore]
            public string DefaultConfigFileExtension { get { return _defaultConfigFileExtension; } private set { _defaultConfigFileExtension = value; } }

            public Config()
            {
                DefaultLookUpFoldersStorage = new Dictionary<string, string>() {
                    { "current", @"." },
                    { "<program>", null },
                    //{ "<library>", @"" },
                    { "rm", @"\rm" },
                    { "nvram", @"\nvram" }
                };
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("LookUpFolders:");
                sb.AppendLine();
                foreach (var kv in _defaultLookUpFolders)
                {
                    sb.AppendFormat("\t{0}:\t{1}", kv.Key, kv.Value);
                    sb.AppendLine();
                }
                sb.Append("CompiledLookUpFolders:");
                sb.AppendLine();
                foreach (string folder in LookUpFolders)
                {
                    sb.AppendFormat("\t{0}", folder);
                    sb.AppendLine();
                }
                sb.AppendFormat("DefaultConfigFileNamePattern: {0}", DefaultConfigFileNamePattern);
                sb.AppendLine();
                sb.AppendFormat("DefaultConfigFileExtension: {0}", DefaultConfigFileExtension);
                return sb.ToString();
            }
        }

        private static Config _defaultConfig = new Config();
        public static Config DefaultConfig { get { return _defaultConfig; } private set { _defaultConfig = value; } }

        public static void InitializeLibraryDefaults()
        {
            string codeBase = Assembly.GetExecutingAssembly().GetName().CodeBase;
            Uri uri = new Uri(codeBase);
            string path = Uri.UnescapeDataString(uri.LocalPath);
            string configFileName = Path.ChangeExtension(path, ".json");
            if (File.Exists(configFileName))
            {
                using (StreamReader streamReader = File.OpenText(configFileName))
                using (JsonReader jsonReader = new JsonTextReader(streamReader))
                {
                    JsonSerializer jsonSerializer = new JsonSerializer();
                    _defaultConfig = jsonSerializer.Deserialize<Config>(jsonReader);
                }
            }
        }

        public static string LookUpConfig(string configName, string[] lookupFolders, uint? appId)
        {
            string[] lookUpFileNames = MakeLookupFileNames(configName, appId);

            // if rooted - path is absolute, just check for existence
            if (Path.IsPathRooted(configName))
            {
                foreach (string lookUpFileName in lookUpFileNames)
                    if (File.Exists(lookUpFileName))
                        return lookUpFileName;
            }
            else
                foreach (string lookUpFileNameInFolder in MakeLookupFileNamesInFolders(lookUpFileNames, lookupFolders, appId))
                    if (File.Exists(lookUpFileNameInFolder))
                        return lookUpFileNameInFolder;

            return null;
        }

        public static JContainer LoadMergedJsonConfig(string configName, string[] lookupFolders, uint? appId)
        {
            string[] lookUpConfigFiles = LookUpConfigs(configName, lookupFolders, appId);
            JContainer configJson = null;
            for(int i = lookUpConfigFiles.Length - 1; i >= 0; i--)
            {
                using (StreamReader streamReader = new StreamReader(lookUpConfigFiles[i]))
                using (JsonReader jsonReader = new JsonTextReader(streamReader))
                {
                    JContainer readJson = (JContainer)JToken.ReadFrom(jsonReader);
                    if (configJson == null)
                        configJson = readJson;
                    else
                        configJson.Merge(readJson);
                }
            }
            return configJson;
        }

        public static T LoadMergedJsonConfig<T>(string configName, string[] lookupFolders, uint? appId)
        {
            JContainer configJson = LoadMergedJsonConfig(configName, lookupFolders, appId);
            return configJson.ToObject<T>();
        }

        public static string[] LookUpConfigs(string configName, string[] lookupFolders, uint? appId)
        {
            List<string> configFiles = new List<string>();
            string[] lookUpFileNames = MakeLookupFileNames(configName, appId);

            // if rooted - path is absolute, just check for existence
            if (Path.IsPathRooted(configName))
            {
                foreach (string lookUpFileName in lookUpFileNames)
                    if (File.Exists(lookUpFileName))
                        configFiles.Add(lookUpFileName);

            }
            else
                foreach (string lookUpFileNameInFolder in MakeLookupFileNamesInFolders(lookUpFileNames, lookupFolders, appId))
                {
                    if (File.Exists(lookUpFileNameInFolder))
                        configFiles.Add(lookUpFileNameInFolder);
                }

            return configFiles.ToArray();
        }

        /// <summary>
        /// Generated array of normalized to lookup filenames based on appId supplied and/or missing extensions
        /// </summary>
        /// <param name="configName">Generic config name, could be specific or just name</param>
        /// <param name="appId">AppId for the config if desired</param>
        /// <returns>array of normalized filenames with extension and appId embeded in the name, starting from more specific</returns>
        internal static string[] MakeLookupFileNames(string configName, uint? appId)
        {
            List<string> lookupFileNames = new List<string>(appId.HasValue ? 2 : 1);
#if SSHARP
            string path = Path.GetDirectoryName(configName);
#else
            string root = Path.GetPathRoot(configName);
            string directory = Path.GetDirectoryName(configName);
            string path = Path.Combine(root, directory);
#endif
            string fileName = Path.GetFileNameWithoutExtension(configName);
            string extension = Path.GetExtension(configName);
            if (String.IsNullOrEmpty(extension))
                extension = DefaultConfig.DefaultConfigFileExtension;

            if (appId.HasValue)
                lookupFileNames.Add(Path.Combine(path, String.Format(DefaultConfig.DefaultConfigFileNamePattern, fileName, appId.Value) + extension));
            lookupFileNames.Add(Path.Combine(path, fileName + extension));

            return lookupFileNames.ToArray();
        }

        internal static string[] MakeLookupFileNamesInFolders(string[] lookUpFileNames, string[] lookupFolders, uint? appid)
        {
            if (lookupFolders == null)
                lookupFolders = DefaultConfig.LookUpFolders;

            List<string> fileNames = new List<string>(lookUpFileNames.Length * lookupFolders.Length);
            foreach(string lookUpFileName in lookUpFileNames)
                foreach (string folder in lookupFolders)
                {
                    fileNames.Add(Path.Combine(folder, lookUpFileName));
                }

            return fileNames.ToArray();
        }
    }
}
