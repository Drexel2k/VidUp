using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

//todo: genereal: add tests for uploading and publish at schedule

namespace Drexel.VidUp.Test
{
    public static class TestHelpers
    {
        public static void CopyTestConfigAndSetCurrentSettings(string fullQualifiedClassName, string testMethodName)
        {
            TestHelpers.getConfigSource(fullQualifiedClassName, testMethodName, out string configSource, out string fullTestName);
            if (string.IsNullOrWhiteSpace(configSource))
            {
                configSource = fullTestName;
            }

            BaseSettings.SubFolder = fullTestName;
            Directory.CreateDirectory(BaseSettings.StorageFolder);

            TestHelpers.copyFile(configSource, BaseSettings.StorageFolder, "templatelist.json");
            TestHelpers.copyFile(configSource, BaseSettings.StorageFolder, "uploadlist.json");
            TestHelpers.copyFile(configSource, BaseSettings.StorageFolder, "uploads.json");
            TestHelpers.copyFile(configSource, BaseSettings.StorageFolder, "playlistlist.json");
        }

        private static void copyFile(string fullTestName, string testStorageFolder, string fileName)
        {
            string jsonFilePath =
                Path.Combine(TestContext.CurrentContext.TestDirectory, "TestConfigs", fullTestName, fileName);
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                json = json.Replace("{BaseDir}", TestContext.CurrentContext.TestDirectory.Replace("\\", "\\\\"));
                File.WriteAllText(Path.Combine(testStorageFolder, fileName), json);
            }
        }

        private static void getConfigSource(string fullQualifiedClassName, string testMethodName, out string configSource, out string fullTestName)
        {
            fullTestName = TestHelpers.getTestName(fullQualifiedClassName, testMethodName);

            configSource = null;
            MethodBase method = Type.GetType(fullQualifiedClassName).GetMethod(testMethodName);
            ConfigSourceAttribute[] attributes =
                (ConfigSourceAttribute[])method.GetCustomAttributes(typeof(ConfigSourceAttribute), true);
            if (attributes.Length > 0)
            {
                ConfigSourceAttribute attribute = attributes[0];
                configSource = attribute.Source;
            }
        }

        private static string getTestName(string fullQualifiedClassName, string testMethodName)
        {
            int startIndex = fullQualifiedClassName.LastIndexOf('.') + 1;
            string className = fullQualifiedClassName.Substring(startIndex,
                fullQualifiedClassName.Length - startIndex);
            return string.Format("{0}_{1}", className, testMethodName);
        }
    }
}
