#region

using System.Collections.Generic;
using System.IO;
using Drexel.VidUp.UI.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

#endregion

namespace Drexel.VidUp.Test
{
    [TestFixture]
    public class RemoveUploadsTests
    {
        private static string t1RootFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "T1Root");
        private static string video1SourceFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestAssets", "video1.mkv");
        private static string video1TargetFilePath = Path.Combine(Path.Combine(RemoveUploadsTests.t1RootFolder, "videos", "video1.mkv"));
        private static string video2SourceFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestAssets", "video2.mkv");
        private static string video2TargetFilePath = Path.Combine(Path.Combine(RemoveUploadsTests.t1RootFolder, "videos", "video2.mkv"));

        [OneTimeSetUp]
        public static void Initialize()
        {
            if (Directory.Exists(BaseSettings.StorageFolder))
            {
                Directory.Delete(BaseSettings.StorageFolder, true);
            }

            if (Directory.Exists(RemoveUploadsTests.t1RootFolder))
            {
                Directory.Delete(RemoveUploadsTests.t1RootFolder, true);
            }

            Directory.CreateDirectory(RemoveUploadsTests.t1RootFolder);
            Directory.CreateDirectory(Path.Combine(RemoveUploadsTests.t1RootFolder, "videos"));

            File.Copy(RemoveUploadsTests.video1SourceFilePath, RemoveUploadsTests.video1TargetFilePath);
            File.Copy(RemoveUploadsTests.video2SourceFilePath, RemoveUploadsTests.video2TargetFilePath);
        }

        [OneTimeTearDown]
        public static void CleanUp()
        {
            if (Directory.Exists(BaseSettings.StorageFolder))
            {
                Directory.Delete(BaseSettings.StorageFolder, true);
            }

            if (Directory.Exists(RemoveUploadsTests.t1RootFolder))
            {
                Directory.Delete(RemoveUploadsTests.t1RootFolder, true);
            }
        }

        [SetUp]
        public static void TestInitialize()
        {
            TestHelpers.CopyTestConfigAndSetCurrentSettings(TestContext.CurrentContext.Test.ClassName, TestContext.CurrentContext.Test.MethodName);
        }

        [Test]
        public static void RemoveFinishedAllTemplates()
        {
            string uploadVideo5GuidWithoutTemplate = "c4af55c4-10e4-4676-9b66-a5d9b7d94afe";
            string uploadVideo6GuidWithTemplate789 = "ff328893-5e6c-4e00-b3d2-7dce42e474e7";
            string uploadVideo2GuidWithTemplate456 = "4be32408-92c0-4d48-8a27-5d98305c79cd";
            string template789Guid = "e2188eed-589d-4233-862c-bc2380cbaf01";
            string template456Guid = "19f37916-68e6-4425-af12-23af87b513c7";

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out _);

            Dictionary<string, int> templateUploadsCount = new Dictionary<string, int>();
            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                templateUploadsCount.Add((string)((JValue)template["guid"]).Value, uploadsJArray.Count);
            }


            mainWindowViewModel.RemoveUploads();
            mainWindowViewModel = null;

            //only uploads with assigned template shall remain left in uploads.json after finished uploads are removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 9);

            bool found = false;

            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo5GuidWithoutTemplate)
                {
                    found = true;
                }

            }

            Assert.IsTrue(!found);

            found = false;
            bool foundAgain = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo6GuidWithTemplate789)
                {
                    if (found)
                    {
                        foundAgain = true;
                    }
                    found = true;
                    string templateGuid = upload["template"].Value<string>();
                    Assert.IsTrue(templateGuid == template789Guid);
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);

            found = false;
            foundAgain = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo2GuidWithTemplate456)
                {
                    if (found)
                    {
                        foundAgain = true;
                    }
                    found = true;
                    string templateGuid = upload["template"].Value<string>();
                    Assert.IsTrue(templateGuid == template456Guid);
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);


            //all finished uploads shall be removed from uploadlist.json after finished uploads are removed
            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 7);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo5GuidWithoutTemplate)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo6GuidWithTemplate789)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo2GuidWithTemplate456)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            //uploads with assigned template shall remain left in template's uploads in templatelist.json after finished uploads are removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));

            Assert.IsTrue(jArray.Count == 6);

            //after removal upload count in each template should be the same
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                Assert.IsTrue(templateUploadsCount[(string)((JValue)template["guid"]).Value] == uploadsJArray.Count);
            }

            bool foundInWrongTemplate = false;
            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo5GuidWithoutTemplate)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foundAgain = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo6GuidWithTemplate789)
                    {
                        if (found)
                        {
                            foundAgain = true;
                        }
                        found = true;

                        if ((string)((JValue)template["guid"]).Value != template789Guid)
                        {
                            foundInWrongTemplate = true;
                        }
                    }
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);
            Assert.IsTrue(!foundInWrongTemplate);

            found = false;
            foundAgain = false;
            foundInWrongTemplate = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo2GuidWithTemplate456)
                    {
                        if (found)
                        {
                            foundAgain = true;
                        }
                        found = true;

                        if ((string)((JValue)template["guid"]).Value != template456Guid)
                        {
                            foundInWrongTemplate = true;
                        }
                    }
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);
            Assert.IsTrue(!foundInWrongTemplate);
        }

        [ConfigSource("RemoveUploadsTests_RemoveFinishedAllTemplates")]
        [Test]
        public static void RemoveFinishedOneTemplate()
        {
            string template789FilterGuid = "e2188eed-589d-4233-862c-bc2380cbaf01";
            string uploadVideo6Guid = "ff328893-5e6c-4e00-b3d2-7dce42e474e7";

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out _);

            Dictionary<string, int> templateUploadsCount = new Dictionary<string, int>();
            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                templateUploadsCount.Add((string)((JValue)template["guid"]).Value, uploadsJArray.Count);
            }

            mainWindowViewModel.RemoveSelectedTemplate =
                mainWindowViewModel.RemoveTemplateViewModels.Find(vm => vm.Guid == template789FilterGuid);
            mainWindowViewModel.RemoveUploads();
            mainWindowViewModel = null;

            //only uploads with assigned template shall remain left in uploads.json after finished uploads are removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 10);

            bool found = false;
            bool foundAgain = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo6Guid)
                {
                    if (found)
                    {
                        foundAgain = true;
                    }
                    found = true;
                    string templateGuid = upload["template"].Value<string>();
                    Assert.IsTrue(templateGuid == template789FilterGuid);
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);


            //all finished uploads shall be removed from uploadlist.json after finished uploads are removed
            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 9);

            found = false;
            foundAgain = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo6Guid)
                {
                    if (found)
                    {
                        foundAgain = true;
                    }
                    found = true;
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(!found);

            //uploads with assigned template shall remain left in template's uploads in templatelist.json after finished uploads are removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));

            Assert.IsTrue(jArray.Count == 6);

            //after removal upload count in each template should be the same
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                Assert.IsTrue(templateUploadsCount[(string)((JValue)template["guid"]).Value] == uploadsJArray.Count);
            }

            bool foundInWrongTemplate = false;
            found = false;
            foundAgain = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo6Guid)
                    {
                        if (found)
                        {
                            foundAgain = true;
                        }

                        found = true;

                        if ((string)((JValue)template["guid"]).Value != template789FilterGuid)
                        {
                            foundInWrongTemplate = true;
                        }
                    }
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);
            Assert.IsTrue(!foundInWrongTemplate);
        }

        [ConfigSource("RemoveUploadsTests_RemoveFinishedAllTemplates")]
        [Test]
        public static void RemoveFinishedWithoutTemplate()
        {
            string uploadVideo5Guid = "c4af55c4-10e4-4676-9b66-a5d9b7d94afe";

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out _);

            Dictionary<string, int> templateUploadsCount = new Dictionary<string, int>();
            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                templateUploadsCount.Add((string)((JValue)template["guid"]).Value, uploadsJArray.Count);
            }

            mainWindowViewModel.RemoveSelectedTemplate =
                mainWindowViewModel.RemoveTemplateViewModels.Find(vm => vm.Template.Name == "None");
            mainWindowViewModel.RemoveUploads();
            mainWindowViewModel = null;

            //only uploads with assigned template shall remain left in uploads.json after finished uploads are removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 9);

            bool found = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo5Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);


            //all finished uploads shall be removed from uploadlist.json after finished uploads are removed
            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 9);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo5Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            //uploads with assigned template shall remain left in template's uploads in templatelist.json after finished uploads are removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));

            Assert.IsTrue(jArray.Count == 6);

            //after removal upload count in each template should be the same
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                Assert.IsTrue(templateUploadsCount[(string)((JValue)template["guid"]).Value] == uploadsJArray.Count);
            }

            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo5Guid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);
        }

        [ConfigSource("RemoveUploadsTests_RemoveFinishedAllTemplates")]
        [Test]
        public static void RemovePausedWithTemplate()
        {
            string template234FilterGuid = "2f80c321-2261-4726-a764-3870ac9340a5";
            string uploadVideo3Guid = "e0acc743-0c49-4dd7-ba4a-093b6686e43b";

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out _);

            Dictionary<string, int> templateUploadsCount = new Dictionary<string, int>();
            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                templateUploadsCount.Add((string)((JValue)template["guid"]).Value, uploadsJArray.Count);
            }

            mainWindowViewModel.RemoveUploadStatus = mainWindowViewModel.RemoveUploadStatuses[2];
            mainWindowViewModel.RemoveSelectedTemplate =
                mainWindowViewModel.RemoveTemplateViewModels.Find(vm => vm.Guid == template234FilterGuid);
            mainWindowViewModel.RemoveUploads();
            mainWindowViewModel = null;

            //only uploads with assigned template shall remain left in uploads.json after finished uploads are removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 9);

            bool found = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo3Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);


            //all finished uploads shall be removed from uploadlist.json after finished uploads are removed
            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 9);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo3Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            //uploads with assigned template shall remain left in template's uploads in templatelist.json after finished uploads are removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));

            Assert.IsTrue(jArray.Count == 6);

            //after removal upload count in template 234 should be one less and on other templates the same
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                string templateGuid = (string) ((JValue) template["guid"]).Value;
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                if (templateGuid == template234FilterGuid)
                {
                    Assert.IsTrue(templateUploadsCount[templateGuid] == uploadsJArray.Count +1);
                }
                else
                {
                    Assert.IsTrue(templateUploadsCount[templateGuid] == uploadsJArray.Count);
                }
            }

            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo3Guid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);
        }

        [ConfigSource("RemoveUploadsTests_RemoveFinishedAllTemplates")]
        [Test]
        public static void RemoveReadyForUploadAllTemplates()
        {
            string uploadVideo1Guid = "da2ede66-e9f9-415a-8fe0-007b8a168d36";
            string uploadVideo4Guid = "e9d3be6a-c987-4b63-be32-e075a7e8a129";
            string uploadVideo7Guid = "9dcd58c4-ef2a-427a-8629-44a812404611";
            string uploadVideo8Guid = "cb3ac8aa-4297-4d3c-9d09-4de6743a84e3";
            string uploadVideo9Guid = "aee98f41-c765-4bc7-91cd-a1ee40388bad";
            string template567video4video7Guid = "7f258a2d-171e-4cf0-8ba6-89966929b742";
            string template234video8Guid = "2f80c321-2261-4726-a764-3870ac9340a5";
            string template456video1Guid = "19f37916-68e6-4425-af12-23af87b513c7";

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out _);

            Dictionary<string, int> templateUploadsCount = new Dictionary<string, int>();
            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                templateUploadsCount.Add((string)((JValue)template["guid"]).Value, uploadsJArray.Count);
            }

            mainWindowViewModel.RemoveUploadStatus = mainWindowViewModel.RemoveUploadStatuses[1];

            mainWindowViewModel.RemoveUploads();
            mainWindowViewModel = null;

            //all uploads ready to uplaod shall be removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 5);

            bool found = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo1Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo4Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo7Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo8Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo9Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);
            
            //all ready to upload uploads shall be removed from uploadlist.json
            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 5);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo1Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo4Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo7Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo8Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo9Guid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            //uploads with assigned template shall be removed in template's uploads in templatelist.json
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));

            Assert.IsTrue(jArray.Count == 6);

            //after removal upload count in each template should be the same
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                string templateGuid = (string) ((JValue) template["guid"]).Value;
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");

                if (templateGuid == template234video8Guid || templateGuid == template456video1Guid)
                {
                    Assert.IsTrue(templateUploadsCount[templateGuid] == uploadsJArray.Count + 1);
                }
                else if (templateGuid == template567video4video7Guid)
                {
                    Assert.IsTrue(templateUploadsCount[templateGuid] == uploadsJArray.Count + 2);
                }
                else
                {
                    Assert.IsTrue(templateUploadsCount[templateGuid] == uploadsJArray.Count);
                }
            }

            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo1Guid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo4Guid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo7Guid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo8Guid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);

            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo9Guid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);
        }

        [ConfigSource("RemoveUploadsTests_RemoveFinishedAllTemplates")]
        [Test]
        public static void RemoveAllWithOneTemplate()
        {
            string template456Guid = "19f37916-68e6-4425-af12-23af87b513c7";
            string uploadVideo1GuidReadyForUpload = "da2ede66-e9f9-415a-8fe0-007b8a168d36";
            string uploadVideo2GuidFinished = "4be32408-92c0-4d48-8a27-5d98305c79cd";

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out _);

            Dictionary<string, int> templateUploadsCount = new Dictionary<string, int>();
            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                templateUploadsCount.Add((string)((JValue)template["guid"]).Value, uploadsJArray.Count);
            }

            mainWindowViewModel.RemoveUploadStatus = mainWindowViewModel.RemoveUploadStatuses[0];
            mainWindowViewModel.RemoveSelectedTemplate =
                mainWindowViewModel.RemoveTemplateViewModels.Find(vm => vm.Guid == template456Guid);
            mainWindowViewModel.RemoveUploads();
            mainWindowViewModel = null;

            //only uploads with assigned template shall remain left in uploads.json after finished uploads are removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 9);

            bool found = false;
            bool foundAgain = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo2GuidFinished)
                {
                    if (found)
                    {
                        foundAgain = true;
                    }
                    found = true;
                    string templateGuid = upload["template"].Value<string>();
                    Assert.IsTrue(templateGuid == template456Guid);
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);

            found = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo1GuidReadyForUpload)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);


            //all uploads from template shall be removed from uploadlist.json 
            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 8);

            found = false;
            foundAgain = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo2GuidFinished)
                {
                    if (found)
                    {
                        foundAgain = true;
                    }
                    found = true;
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(!found);

            found = false;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                if ((string)guid.Value == uploadVideo1GuidReadyForUpload)
                {
                    found = true;
                }
            }

            Assert.IsTrue(!found);

            //uploads with assigned template shall remain left in template's uploads in templatelist.json 
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));

            Assert.IsTrue(jArray.Count == 6);

            //after removal upload count in each template should be the same
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                string templateGuid = (string)((JValue)template["guid"]).Value;
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");

                if (templateGuid == template456Guid)
                {
                    Assert.IsTrue(templateUploadsCount[templateGuid] == uploadsJArray.Count + 1);
                }
                else
                {
                    Assert.IsTrue(templateUploadsCount[templateGuid] == uploadsJArray.Count);
                }
            }

            bool foundInWrongTemplate = false;
            found = false;
            foundAgain = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo2GuidFinished)
                    {
                        if (found)
                        {
                            foundAgain = true;
                        }

                        found = true;

                        if ((string)((JValue)template["guid"]).Value != template456Guid)
                        {
                            foundInWrongTemplate = true;
                        }
                    }
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);
            Assert.IsTrue(!foundInWrongTemplate);

            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo1GuidReadyForUpload)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);
        }

        [ConfigSource("RemoveUploadsTests_RemoveFinishedAllTemplates")]
        [Test]
        public static void RemoveAll()
        {
            string uploadVideo1Guid = "da2ede66-e9f9-415a-8fe0-007b8a168d36";
            string uploadVideo2GuidFinished = "4be32408-92c0-4d48-8a27-5d98305c79cd";
            string uploadVideo4Guid = "e9d3be6a-c987-4b63-be32-e075a7e8a129";
            string uploadVideo5GuidFinished = "c4af55c4-10e4-4676-9b66-a5d9b7d94afe";
            string uploadVideo6GuidFinished = "ff328893-5e6c-4e00-b3d2-7dce42e474e7";
            string uploadVideo7Guid = "9dcd58c4-ef2a-427a-8629-44a812404611";
            string uploadVideo8Guid = "cb3ac8aa-4297-4d3c-9d09-4de6743a84e3";
            string uploadVideo9Guid = "aee98f41-c765-4bc7-91cd-a1ee40388bad";
            string template789GuidVideo6 = "e2188eed-589d-4233-862c-bc2380cbaf01";
            string template456GuidVideo2 = "19f37916-68e6-4425-af12-23af87b513c7";

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out _);

            Dictionary<string, int> templateUploadsCount = new Dictionary<string, int>();
            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                templateUploadsCount.Add((string)((JValue)template["guid"]).Value, uploadsJArray.Count);
            }

            mainWindowViewModel.RemoveUploadStatus = mainWindowViewModel.RemoveUploadStatuses[0];
            mainWindowViewModel.RemoveSelectedTemplate =
                mainWindowViewModel.RemoveTemplateViewModels.Find(vm => vm.Template.Name == "All");
            mainWindowViewModel.RemoveUploads();
            mainWindowViewModel = null;

            //all uploads not finished shall be removed
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 2);

            bool found = false;
            bool foundAgain = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo2GuidFinished)
                {
                    if (found)
                    {
                        foundAgain = true;
                    }
                    found = true;
                    string templateGuid = upload["template"].Value<string>();
                    Assert.IsTrue(templateGuid == template456GuidVideo2);
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);

            found = false;
            foundAgain = false;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                string guid = upload["guid"].Value<string>();
                if (guid == uploadVideo6GuidFinished)
                {
                    if (found)
                    {
                        foundAgain = true;
                    }
                    found = true;
                    string templateGuid = upload["template"].Value<string>();
                    Assert.IsTrue(templateGuid == template789GuidVideo6);
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);

            //all ready to upload uploads shall be removed from uploadlist.json
            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 0);

            //uploads with assigned template shall be removed in template's uploads in templatelist.json
            jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));

            Assert.IsTrue(jArray.Count == 6);

            //after removal upload count in each template should be 0 except finished uploads
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                string templateGuid = (string)((JValue)template["guid"]).Value;
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");

                if (templateGuid == template456GuidVideo2 || templateGuid == template789GuidVideo6)
                {
                    Assert.IsTrue(uploadsJArray.Count == 1);
                }
                else
                {
                    Assert.IsTrue(uploadsJArray.Count == 0);
                }
            }

            bool foundInWrongTemplate = false;
            found = false;
            foundAgain = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo2GuidFinished)
                    {
                        if (found)
                        {
                            foundAgain = true;
                        }

                        found = true;

                        if ((string)((JValue)template["guid"]).Value != template456GuidVideo2)
                        {
                            foundInWrongTemplate = true;
                        }
                    }
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);
            Assert.IsTrue(!foundInWrongTemplate);

            found = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo5GuidFinished)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(!found);

            foundInWrongTemplate = false;
            found = false;
            foundAgain = false;
            foreach (JObject template in jArray.SelectTokens("$.[*]"))
            {
                JArray uploadsJArray = (JArray)template.SelectToken("$.uploads");
                foreach (JValue uploadGuid in uploadsJArray.SelectTokens("$.[*]"))
                {
                    if ((string)uploadGuid.Value == uploadVideo6GuidFinished)
                    {
                        if (found)
                        {
                            foundAgain = false;
                        }

                        found = true;

                        if ((string)((JValue)template["guid"]).Value != template789GuidVideo6)
                        {
                            foundInWrongTemplate = true;
                        }
                    }
                }
            }

            Assert.IsTrue(!foundAgain);
            Assert.IsTrue(found);
            Assert.IsTrue(!foundInWrongTemplate);
        }
    }
}
