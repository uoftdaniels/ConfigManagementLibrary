using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace Daniels.Config
{
    public static class ConfigManagement
    {
        private static Dictionary<string, string> _defaultLookupFolders = new Dictionary<string, string>() {
            { "current", @"." },
            { "rm", @"\rm" },
            { "nvram", @"\nvram" }
        };

        public static IReadOnlyDictionary<string, string> DefaultLookupFolders
        {
            get
            {
                return _defaultLookupFolders;
            }
        }

        public static string DefaultConfigFileNamePattern = @"{0}-P{1:D2}";
        public static string DefaultConfigFileExtension = @".json";


        public static string LookupConfigForSlot(uint slot, string configName, IReadOnlyDictionary<string, string> lookupFolders)
        {
            // if rooted - path is absolute, just check for existence
            if (Path.IsPathRooted(configName))
            {
                if (File.Exists(configName))
                    return configName;
                else
                    return null;
            }

            if (lookupFolders == null)
                return LookupConfigInFolders(configName, DefaultLookupFolders);
            else
                return LookupConfigInFolders(configName, lookupFolders);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="lookupFolders"></param>
        /// <returns></returns>
        public static string LookupConfig(string configName, IReadOnlyDictionary<string, string> lookupFolders)
        {
            // if rooted - path is absolute, just check for existence
            if (Path.IsPathRooted(configName))
            {
                if (File.Exists(configName))
                    return configName;
                else
                    return null;
            }

            if (lookupFolders == null)
                return LookupConfigInFolders(configName, DefaultLookupFolders);
            else
                return LookupConfigInFolders(configName, lookupFolders);
        }

        internal static string MakeLookupFileName(uint? appId, string configName)
        {
            string fileName = Path.GetFileNameWithoutExtension(configName);
            string extension = Path.GetExtension(configName);
            if (String.IsNullOrEmpty(extension))
                extension = DefaultConfigFileExtension;
            string directory = Path.GetDirectoryName(configName);

            //IReadOnlyDictionary<string, string> lookupFolders;
            string lookupFileName;
            if (String.IsNullOrEmpty(directory))
                lookupFileName = Path.Combine(fileName, extension);
            else
#if SSHARP
                lookupFileName = Path.Combine(directory, Path.Combine(fileName, extension));
#else
                lookupFileName = Path.Combine(directory, Path.ChangeExtension((appId.HasValue)? String.Format(DefaultConfigFileNamePattern, fileName, appId.Value) : fileName, extension));
#endif
            return lookupFileName;
        }

        /// <summary>
        /// Check if supplied config exists on filesystem according to defined search folders
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="lookupFolders"></param>
        /// <returns>Rooted path to the file if file exists or null if not</returns>
        private static string LookupConfigInFolders(string lookupFileName, IReadOnlyDictionary<string, string> lookupFolders)
        {
            foreach (string folderKey in lookupFolders.Keys)
            {
                string lookupFileNameInFolder = Path.Combine(lookupFolders[folderKey], lookupFileName);
                if (File.Exists(lookupFileNameInFolder))
                    return lookupFileNameInFolder;
            }

            return null;
        }
    }
}
