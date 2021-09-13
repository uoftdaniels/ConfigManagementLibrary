using System;
using System.Collections.Generic;

#if SSHARP
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Reflection;
#else
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
#endif

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Daniels.Config
{
    public static class ConfigManagement
    {
        public class Config
        {
            [JsonProperty("DefaultLookUpFolders")]
            internal readonly Dictionary<string, string> _defaultLookUpFolders = new Dictionary<string, string>() {
                { "current", @"." },
                { "executing", @"" },
                { "rm", @"\rm" },
                { "nvram", @"\nvram" }
            };

            [JsonIgnore]
            public string[] DefaultLookUpFolders { get { string[] folders = new string[_defaultLookUpFolders.Count]; _defaultLookUpFolders.Values.CopyTo(folders, 0); return folders; } }

            [JsonProperty("DefaultConfigFileNamePattern")]
            private string _defaultConfigFileNamePattern = @"{0}-P{1:D2}";
            [JsonIgnore]
            public string DefaultConfigFileNamePattern { get { return _defaultConfigFileNamePattern; } private set { _defaultConfigFileNamePattern = value; } }

            [JsonProperty("DefaultConfigFileExtension")]
            private string _efaultConfigFileExtension = @".json";
            [JsonIgnore]
            public string DefaultConfigFileExtension { get { return _efaultConfigFileExtension; } private set { _efaultConfigFileExtension = value; } }

            public Config()
            {
                List<string> folderKeys = new List<string>(_defaultLookUpFolders.Keys);
                foreach (string folderKey in folderKeys)
                {
                    switch (folderKey)
                    {
                        case "executing":
#if SSHARP
                            _defaultLookUpFolders[folderKey] = Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().GetName().CodeBase).LocalPath);
#else
                            _defaultLookUpFolders[folderKey] = AppDomain.CurrentDomain.BaseDirectory;
#endif
                            break;
                    }
                }
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

        public static string LookupConfig(string configName, string[] lookupFolders, uint? appId)
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
                    if (File.Exists(lookUpFileNameInFolder))
                        configFiles.Add(lookUpFileNameInFolder);

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
                lookupFolders = DefaultConfig.DefaultLookUpFolders;

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
