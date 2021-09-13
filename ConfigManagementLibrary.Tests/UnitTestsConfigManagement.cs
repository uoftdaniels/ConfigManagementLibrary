using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using Newtonsoft.Json.Linq;

using Daniels.Config;

namespace Daniels.Config.Tests
{
    [TestFixture]
    class ConfigManagementTests
    {

        [SetUp]
        public void Setup()
        {
            ConfigManagement.InitializeLibraryDefaults();

        }

        [TestCase(@"testConfig", null)]
        [TestCase(@"testConfig", 1u)]
        [TestCase(@"testConfig.xml", 2u)]
        [TestCase(@"folder\testConfig", 3u)]
        [TestCase(@"\folder\testConfig.conf", 4u)]
        public void MakeLookupFileNamesTest(string configName, uint? appId)
        {
            string[] lookUpFileNames = ConfigManagement.MakeLookupFileNames(configName, appId);
            foreach (string lookUpFileName in lookUpFileNames)
                Console.WriteLine("LookupFileName: {0}", lookUpFileName);

            if(appId.HasValue)
                Assert.That(lookUpFileNames.Length == 2, Is.True, "With appId supplied, two filenames should be returned");
            else
                Assert.That(lookUpFileNames.Length == 1, Is.True, "With appId not supplied, one filename should be returned");

            if(appId.HasValue)
                switch(appId.Value)
                {
                    case 1u:
                        Assert.That(lookUpFileNames[0], Is.EqualTo("testConfig-P01.json"));
                        Assert.That(lookUpFileNames[1], Is.EqualTo("testConfig.json"));
                        break;
                    case 2u:
                        Assert.That(lookUpFileNames[0], Is.EqualTo("testConfig-P02.xml"));
                        Assert.That(lookUpFileNames[1], Is.EqualTo("testConfig.xml"));
                        break;
                    case 3u:
                        Assert.That(lookUpFileNames[0], Is.EqualTo(@"folder\testConfig-P03.json"));
                        Assert.That(lookUpFileNames[1], Is.EqualTo(@"folder\testConfig.json"));
                        break;
                    case 4u:
                        Assert.That(lookUpFileNames[0], Is.EqualTo(@"\folder\testConfig-P04.conf"));
                        Assert.That(lookUpFileNames[1], Is.EqualTo(@"\folder\testConfig.conf"));
                        break;
                }
            else
                Assert.That(lookUpFileNames[0], Is.EqualTo("testConfig.json"));
        }

        [TestCase(@"testConfig", null)]
        [TestCase(@"testConfig", 1u)]
        [TestCase(@"testConfig.xml", 2u)]
        [TestCase(@"folder\testConfig", 3u)]
        [TestCase(@"\folder\testConfig.conf", 4u)]
        public void MakeLookupFileNamesInFoldersTest(string configName, uint? appId)
        {
            string[] lookUpFileNames = ConfigManagement.MakeLookupFileNames(configName, appId);

            string[] lookUpFileNamesInFolders = ConfigManagement.MakeLookupFileNamesInFolders(lookUpFileNames, null, appId);
            foreach (string lookUpFileName in lookUpFileNamesInFolders)
                Console.WriteLine("LookupFileName: {0}", lookUpFileName);

        }

        [TestCase(@"testConfig", 1u)]
        public void LookUpConfigsTest(string configName, uint? appId)
        {
            string[] configFiles = ConfigManagement.LookUpConfigs(configName, null, appId);
            foreach (string configFile in configFiles)
                Console.WriteLine("LookupFileName: {0}", configFile);
        }

        [TestCase(@"testConfig", 1u)]
        public void LoadMergedJsonConfigTest(string configName, uint? appId)
        {
            JObject configJson = (JObject)ConfigManagement.LoadMergedJsonConfig(configName, null, appId);
            Assert.That(configJson.ContainsKey("TestGlobal"), Is.True);
            Assert.That(configJson.ContainsKey("TestLocal"), Is.True);
            Assert.That(configJson["TestGlobalOverrided"].ToString(), Is.EqualTo("LocalOverrided"));
            Console.WriteLine("MergedJson: {0}", configJson);
        }
    }
}
