using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Drexel.VidUp.Test
{
    public class BasicTests
    {
        private static string t1RootFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "T1Root");
        private static string t2RootFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "T2Root");
        private static string video1SourceFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestAssets", "video1.mkv");
        private static string video1TargetFilePath = Path.Combine(Path.Combine(BasicTests.t1RootFolder, "videos", "video1.mkv"));
        private static string video2SourceFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestAssets", "video2.mkv");
        private static string video2TargetFilePath = Path.Combine(Path.Combine(BasicTests.t2RootFolder, "videos", "video2.mkv"));
        private static string testTemplateName = "First Template";
        private static string testTemplateTitle = "First Template Title";
        private static string testTemplateDescription = "First Template Description";
        private static string testTemplateTag1 = "Tag 1";
        private static string testTemplateTag2 = "Tag2";
        private static string testTemplateGuid = "61ed37cf-2e7c-4f98-8348-9b469a76fc66";
        private static string defaultTemplateRenderingImageFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"images\defaultupload.png"));
        private static string testPlaylistName = "First Playlist";
        private static string testPlaylistId = "FirstPlaylistId";
        private static string privateVisibility = "Private";

        [OneTimeSetUp]
        public static void Initialize()
        {
            if (Directory.Exists(BaseSettings.StorageFolder))
            {
                Directory.Delete(BaseSettings.StorageFolder, true);
            }

            if (Directory.Exists(BasicTests.t1RootFolder))
            {
                Directory.Delete(BasicTests.t1RootFolder, true);
            }

            if (Directory.Exists(BasicTests.t2RootFolder))
            {
                Directory.Delete(BasicTests.t2RootFolder, true);
            }

            Directory.CreateDirectory(BasicTests.t1RootFolder);
            Directory.CreateDirectory(Path.Combine(BasicTests.t1RootFolder, "videos"));

            Directory.CreateDirectory(BasicTests.t2RootFolder);
            Directory.CreateDirectory(Path.Combine(BasicTests.t2RootFolder, "videos"));

            File.Copy(BasicTests.video1SourceFilePath, BasicTests.video1TargetFilePath);
            File.Copy(BasicTests.video2SourceFilePath, BasicTests.video2TargetFilePath);
        }

        [OneTimeTearDown]
        public static void CleanUp()
        {
            if (Directory.Exists(BaseSettings.StorageFolder))
            {
                Directory.Delete(BaseSettings.StorageFolder, true);
            }

            if (Directory.Exists(BasicTests.t1RootFolder))
            {
                Directory.Delete(BasicTests.t1RootFolder, true);
            }

            if (Directory.Exists(BasicTests.t2RootFolder))
            {
                Directory.Delete(BasicTests.t2RootFolder, true);
            }
        }

        [SetUp]
        public static void TestInitialize()
        {
            TestHelpers.CopyTestConfigAndSetCurrentSettings(TestContext.CurrentContext.Test.ClassName, TestContext.CurrentContext.Test.MethodName);
        }

        [Test]
        public static void FirstStart()
        {
            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            Assert.IsTrue(uploadList.UploadCount == 0);
            Assert.IsTrue(templateList.TemplateCount == 0);
            Assert.IsTrue(playlistList.PlaylistCount == 0);

            uploadList = null;
            templateList = null;
            playlistList = null;
            mainWindowViewModel = null;

            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));
            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json")));
            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "playlistlist.json")));
        }

        [Test]
        public static void AddFirstUpload()
        {
            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            UploadListViewModel uploadListViewModel = (UploadListViewModel) mainWindowViewModel.CurrentViewModel;
            
            List<Upload> uploads = new List<Upload>();
            uploads.Add(new Upload(BasicTests.video1TargetFilePath));
            uploadListViewModel.AddUploads(uploads);

            Assert.IsTrue(uploadList.UploadCount == 1);
            Assert.IsTrue(uploadList.GetUpload(0).FilePath == BasicTests.video1TargetFilePath);
            Assert.IsTrue(templateList.TemplateCount == 0);
            Assert.IsTrue(playlistList.PlaylistCount == 0);

            uploadList = null;
            templateList = null;
            playlistList = null;
            mainWindowViewModel = null;

            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));
            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json")));
            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "playlistlist.json")));

            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 1);

            bool found = false;
            int uploadsCount = 0;
            string newUploadGuid = null;
            foreach (JObject upload in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string filePath = upload["filePath"].Value<string>();
                if (filePath == BasicTests.video1TargetFilePath)
                {
                    newUploadGuid = upload["guid"].Value<string>();
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            uploadsCount = 0;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string jsonGuid = guid.Value<string>();
                if (jsonGuid == newUploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json"))))["templates"];
            Assert.IsTrue(jArray.Count == 0);
        }

        [Test]
        public static void AddFirstTemplate()
        {
            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            mainWindowViewModel.TabNo = 1;
            TemplateViewModel templateViewModel = (TemplateViewModel)mainWindowViewModel.CurrentViewModel;
            templateViewModel.AddTemplate(new Template(BasicTests.testTemplateName, null, BasicTests.t1RootFolder, templateList));

            Assert.IsTrue(uploadList.UploadCount == 0);
            Assert.IsTrue(templateList.TemplateCount == 1);
            Assert.IsTrue(playlistList.PlaylistCount == 0);

            Template template = templateList.GetTemplate(0);
            Assert.IsTrue(template.Name == BasicTests.testTemplateName);
            Assert.IsTrue(Path.GetFullPath(template.ImageFilePathForRendering) == BasicTests.defaultTemplateRenderingImageFilePath);
            Assert.IsTrue(template.ImageFilePathForEditing == null);
            Assert.IsTrue(template.RootFolderPath == BasicTests.t1RootFolder);

            uploadList = null;
            templateList = null;
            playlistList = null;
            mainWindowViewModel = null;

            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));
            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json")));
            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "playlistlist.json")));

            JArray jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json"))))["templates"];

            Assert.IsTrue(jArray.Count == 1);

            bool found = false;
            int templatesCount = 0;
            foreach (JObject jsonTemplate in jArray.SelectTokens("$.[*]"))
            {
                templatesCount++;
                string name = jsonTemplate["name"].Value<string>();
                string imageFilePath = jsonTemplate["imageFilePath"].Value<string>();
                string rootFolderPath = jsonTemplate["rootFolderPath"].Value<string>();
                if (name == BasicTests.testTemplateName)
                {
                    found = true;
                }

                Assert.IsTrue(name == BasicTests.testTemplateName);
                Assert.IsTrue(imageFilePath == null);
                Assert.IsTrue(rootFolderPath == BasicTests.t1RootFolder);
            }

            Assert.IsTrue(templatesCount == 1);
            Assert.IsTrue(found);
        }

        [Test]
        public static void AddFirstPlaylist()
        {
            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            mainWindowViewModel.TabNo = 2;
            PlaylistViewModel playlistViewModel = (PlaylistViewModel)mainWindowViewModel.CurrentViewModel;
            playlistViewModel.AddPlaylist(new Playlist(BasicTests.testPlaylistName, BasicTests.testPlaylistId));

            Assert.IsTrue(uploadList.UploadCount == 0);
            Assert.IsTrue(templateList.TemplateCount == 0);
            Assert.IsTrue(playlistList.PlaylistCount == 1);

            Playlist playlist = playlistList.GetPlaylist(0);
            Assert.IsTrue(playlist.Name == BasicTests.testPlaylistName);
            Assert.IsTrue(playlist.PlaylistId == BasicTests.testPlaylistId);

            uploadList = null;
            templateList = null;
            playlistList = null;
            mainWindowViewModel = null;

            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));
            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json")));
            Assert.IsTrue(!File.Exists(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json")));
            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "playlistlist.json")));

            JArray jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "playlistlist.json"))))["playlists"];

            Assert.IsTrue(jArray.Count == 1);

            bool found = false;
            int playistCount = 0;
            foreach (JObject jsonPlaylist in jArray.SelectTokens("$.[*]"))
            {
                playistCount++;
                string name = jsonPlaylist["name"].Value<string>();
                string playlistId = jsonPlaylist["playlistId"].Value<string>();
                if (name == BasicTests.testPlaylistName)
                {
                    found = true;
                }

                Assert.IsTrue(name == BasicTests.testPlaylistName);
                Assert.IsTrue(playlistId == BasicTests.testPlaylistId);
            }

            Assert.IsTrue(playistCount == 1);
            Assert.IsTrue(found);
        }

        [Test]
        public static void AddUploadWithoutTemplateMatch()
        {
            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            UploadListViewModel uploadListViewModel = (UploadListViewModel)mainWindowViewModel.CurrentViewModel;

            List<Upload> uploads = new List<Upload>();
            Upload upload = new Upload(BasicTests.video2TargetFilePath);
            uploads.Add(upload);
            uploadListViewModel.AddUploads(uploads);

            Assert.IsTrue(upload.Template == null);
            Assert.IsTrue(upload.Description == null);
            Assert.IsTrue(Path.GetFullPath(upload.ImageFilePath) == Path.GetFullPath(BasicTests.defaultTemplateRenderingImageFilePath));
            Assert.IsTrue(upload.Playlist == null);
            Assert.IsTrue(upload.Tags.Count == 0);
            Assert.IsTrue(upload.ThumbnailFilePath == null);
            Assert.IsTrue(upload.Title == "video2");
            Assert.IsTrue(upload.YtTitle == "video2");
            Assert.IsTrue(upload.Visibility == Visibility.Private);

            uploadList = null;
            templateList = null;
            playlistList = null;
            mainWindowViewModel = null;

            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));
            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json")));

            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 1);

            bool found = false;
            int uploadsCount = 0;
            string newUploadGuid = null;
            foreach (JObject jsonUpload in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string filePath = jsonUpload["filePath"].Value<string>();
                string templateGuid = jsonUpload["template"].Value<string>();
                string playlist = jsonUpload["playlist"].Value<string>();
                int tagCount = ((JArray)jsonUpload["tags"]).Count;
                string thumbnailFilePath = jsonUpload["thumbnailFilePath"].Value<string>();
                string title = jsonUpload["title"].Value<string>();
                string visibility = jsonUpload["visibility"].Value<string>();

                if (filePath == BasicTests.video2TargetFilePath)
                {
                    newUploadGuid = jsonUpload["guid"].Value<string>();
                    found = true;
                }

                Assert.IsTrue(templateGuid == null);
                Assert.IsTrue(playlist == null);
                Assert.IsTrue(tagCount == 0);
                Assert.IsTrue(thumbnailFilePath == null);
                Assert.IsTrue(title == "video2");
                Assert.IsTrue(visibility == BasicTests.privateVisibility);
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            uploadsCount = 0;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string jsonGuid = guid.Value<string>();
                if (jsonGuid == newUploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(found);
        }

        [ConfigSource("BasicTests_AddUploadWithoutTemplateMatch")]
        [Test]
        public static void AddUploadWithTemplateMatch()
        {
            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            UploadListViewModel uploadListViewModel = (UploadListViewModel)mainWindowViewModel.CurrentViewModel;

            List<Upload> uploads = new List<Upload>();
            Upload upload = new Upload(BasicTests.video1TargetFilePath);
            uploads.Add(upload);
            uploadListViewModel.AddUploads(uploads);

            Assert.IsTrue(upload.Template == templateList.GetTemplate(0));
            Assert.IsTrue(upload.Description == BasicTests.testTemplateDescription);
            Assert.IsTrue(Path.GetFullPath(upload.ImageFilePath) == Path.GetFullPath(BasicTests.defaultTemplateRenderingImageFilePath));
            Assert.IsTrue(upload.Playlist == playlistList.GetPlaylist(0));
            Assert.IsTrue(upload.Tags.Count == 2);
            Assert.IsTrue(upload.Tags[0] == BasicTests.testTemplateTag1);
            Assert.IsTrue(upload.Tags[1] == BasicTests.testTemplateTag2);
            Assert.IsTrue(upload.ThumbnailFilePath == null);
            //todo: add video with # placeholder
            Assert.IsTrue(upload.Title == BasicTests.testTemplateTitle);
            Assert.IsTrue(upload.YtTitle == BasicTests.testTemplateTitle);
            Assert.IsTrue(upload.Visibility == Visibility.Private);

            uploadList = null;
            templateList = null;
            playlistList = null;
            mainWindowViewModel = null;

            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));
            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json")));

            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 1);

            bool found = false;
            int uploadsCount = 0;
            string newUploadGuid = null;
            foreach (JObject jsonUpload in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string filePath = jsonUpload["filePath"].Value<string>();
                string templateGuid = jsonUpload["template"].Value<string>();
                string playlist = jsonUpload["playlist"].Value<string>();
                int tagCount = ((JArray)jsonUpload["tags"]).Count;
                string tag1 = ((JArray) jsonUpload["tags"])[0].Value<string>();
                string tag2 = ((JArray)jsonUpload["tags"])[1].Value<string>();
                string thumbnailFilePath = jsonUpload["thumbnailFilePath"].Value<string>();
                string title = jsonUpload["title"].Value<string>();
                string visibility = jsonUpload["visibility"].Value<string>();

                if (filePath == BasicTests.video1TargetFilePath)
                {
                    newUploadGuid = jsonUpload["guid"].Value<string>();
                    found = true;
                }

                Assert.IsTrue(templateGuid == BasicTests.testTemplateGuid);
                Assert.IsTrue(playlist == BasicTests.testPlaylistId);
                Assert.IsTrue(tagCount == 2);
                Assert.IsTrue(tag1 == BasicTests.testTemplateTag1);
                Assert.IsTrue(tag2 == BasicTests.testTemplateTag2);
                Assert.IsTrue(thumbnailFilePath == null);
                Assert.IsTrue(title == BasicTests.testTemplateTitle);
                Assert.IsTrue(visibility == BasicTests.privateVisibility);
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            uploadsCount = 0;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string jsonGuid = guid.Value<string>();
                if (jsonGuid == newUploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(found);
        }

        [ConfigSource("BasicTests_AddUploadWithoutTemplateMatch")]
        [Test]
        public static void Add2UploadsWithAndWithoutTemplateMatch()
        {
            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            UploadListViewModel uploadListViewModel = (UploadListViewModel)mainWindowViewModel.CurrentViewModel;

            List<Upload> uploads = new List<Upload>();
            
            Upload uploadWithoutTemplateMatch = new Upload(BasicTests.video2TargetFilePath);
            uploads.Add(uploadWithoutTemplateMatch);
            Upload uploadWithTemplateMatch = new Upload(BasicTests.video1TargetFilePath);
            uploads.Add(uploadWithTemplateMatch);

            uploadListViewModel.AddUploads(uploads);

            Assert.IsTrue(uploadWithoutTemplateMatch.Template == null);
            Assert.IsTrue(uploadWithoutTemplateMatch.Description == null);
            Assert.IsTrue(Path.GetFullPath(uploadWithoutTemplateMatch.ImageFilePath) == Path.GetFullPath(BasicTests.defaultTemplateRenderingImageFilePath));
            Assert.IsTrue(uploadWithoutTemplateMatch.Playlist == null);
            Assert.IsTrue(uploadWithoutTemplateMatch.Tags.Count == 0);
            Assert.IsTrue(uploadWithoutTemplateMatch.ThumbnailFilePath == null);
            Assert.IsTrue(uploadWithoutTemplateMatch.Title == "video2");
            Assert.IsTrue(uploadWithoutTemplateMatch.YtTitle == "video2");
            Assert.IsTrue(uploadWithoutTemplateMatch.Visibility == Visibility.Private);

            Assert.IsTrue(uploadWithTemplateMatch.Template == templateList.GetTemplate(0));
            Assert.IsTrue(uploadWithTemplateMatch.Description == BasicTests.testTemplateDescription);
            Assert.IsTrue(Path.GetFullPath(uploadWithTemplateMatch.ImageFilePath) == Path.GetFullPath(BasicTests.defaultTemplateRenderingImageFilePath));
            Assert.IsTrue(uploadWithTemplateMatch.Playlist == playlistList.GetPlaylist(0));
            Assert.IsTrue(uploadWithTemplateMatch.Tags.Count == 2);
            Assert.IsTrue(uploadWithTemplateMatch.Tags[0] == BasicTests.testTemplateTag1);
            Assert.IsTrue(uploadWithTemplateMatch.Tags[1] == BasicTests.testTemplateTag2);
            Assert.IsTrue(uploadWithTemplateMatch.ThumbnailFilePath == null);
            //todo: add video with # placeholder
            Assert.IsTrue(uploadWithTemplateMatch.Title == BasicTests.testTemplateTitle);
            Assert.IsTrue(uploadWithTemplateMatch.YtTitle == BasicTests.testTemplateTitle);
            Assert.IsTrue(uploadWithTemplateMatch.Visibility == Visibility.Private);

            uploadList = null;
            templateList = null;
            playlistList = null;
            mainWindowViewModel = null;

            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));
            Assert.IsTrue(File.Exists(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json")));

            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 2);

            bool foundUploadWithoutTemplateMatch = false;
            bool foundUploadWithTemplateMatch = false;
            int uploadsCount = 0;
            string uploadWithoutTemplateMatchGuid = null;
            string uploadWithTemplateMatchGuid = null;

            foreach (JObject jsonUpload in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string filePath = jsonUpload["filePath"].Value<string>();
                string templateGuid = jsonUpload["template"].Value<string>();
                string playlist = jsonUpload["playlist"].Value<string>();
                int tagCount = ((JArray)jsonUpload["tags"]).Count;
                string thumbnailFilePath = jsonUpload["thumbnailFilePath"].Value<string>();
                string title = jsonUpload["title"].Value<string>();
                string visibility = jsonUpload["visibility"].Value<string>();

                if (filePath == BasicTests.video2TargetFilePath)
                {
                    uploadWithoutTemplateMatchGuid = jsonUpload["guid"].Value<string>();
                    foundUploadWithoutTemplateMatch = true;

                    Assert.IsTrue(templateGuid == null);
                    Assert.IsTrue(playlist == null);
                    Assert.IsTrue(tagCount == 0);
                    Assert.IsTrue(thumbnailFilePath == null);
                    Assert.IsTrue(title == "video2");
                    Assert.IsTrue(visibility == BasicTests.privateVisibility);
                }

                if (filePath == BasicTests.video1TargetFilePath)
                {
                    uploadWithTemplateMatchGuid = jsonUpload["guid"].Value<string>();
                    foundUploadWithTemplateMatch = true;

                    string tag1 = ((JArray)jsonUpload["tags"])[0].Value<string>();
                    string tag2 = ((JArray)jsonUpload["tags"])[1].Value<string>();

                    Assert.IsTrue(templateGuid == BasicTests.testTemplateGuid);
                    Assert.IsTrue(playlist == BasicTests.testPlaylistId);
                    Assert.IsTrue(tagCount == 2);
                    Assert.IsTrue(tag1 == BasicTests.testTemplateTag1);
                    Assert.IsTrue(tag2 == BasicTests.testTemplateTag2);
                    Assert.IsTrue(thumbnailFilePath == null);
                    Assert.IsTrue(title == BasicTests.testTemplateTitle);
                    Assert.IsTrue(visibility == BasicTests.privateVisibility);
                }
            }

            Assert.IsTrue(uploadsCount == 2);
            Assert.IsTrue(foundUploadWithoutTemplateMatch);
            Assert.IsTrue(foundUploadWithTemplateMatch);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 2);

            foundUploadWithoutTemplateMatch = false;
            foundUploadWithTemplateMatch = false;
            uploadsCount = 0;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string jsonGuid = guid.Value<string>();
                if (jsonGuid == uploadWithoutTemplateMatchGuid)
                {
                    foundUploadWithoutTemplateMatch = true;
                }

                if (jsonGuid == uploadWithTemplateMatchGuid)
                {
                    foundUploadWithTemplateMatch = true;
                }
            }

            Assert.IsTrue(uploadsCount == 2);
            Assert.IsTrue(foundUploadWithoutTemplateMatch);
            Assert.IsTrue(foundUploadWithTemplateMatch);
        }

        [Test]
        public static void RemoveUploadReadyToUploadWithoutTemplate()
        {
            string uploadGuid = "0f179547-8981-4509-b167-b84d1fdbc2e6";

            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            UploadListViewModel uploadListViewModel = (UploadListViewModel)mainWindowViewModel.CurrentViewModel;
            uploadListViewModel.DeleteCommand.Execute(uploadGuid);

            Assert.IsTrue(uploadList.UploadCount == 1);
            Upload upload = uploadList.GetUpload(0);
            Assert.IsTrue(upload.Guid != Guid.Parse(uploadGuid));

            Template template = templateList.GetTemplate(0);
            Assert.IsTrue(template.Uploads.Count == 1);
            Assert.IsTrue(template.Uploads[0].Guid != Guid.Parse(uploadGuid));

            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 1);

            bool found = false;
            int uploadsCount = 0;
            foreach (JObject jsonUpload in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string guid = jsonUpload["guid"].Value<string>();

                if (guid == uploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            uploadsCount = 0;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string jsonGuid = guid.Value<string>();
                if (jsonGuid == uploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json"))))["templates"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            int templatesCount = 0;
            uploadsCount = 0;
            foreach (JObject jsonTemplate in jArray.SelectTokens("$.[*]"))
            {
                templatesCount++;
                JToken uploads = jsonTemplate["uploads"];
                foreach (JToken jsonUploadGuid in uploads.SelectTokens("$.[*]"))
                {
                    uploadsCount++;
                    string guid = jsonUploadGuid.Value<string>();
                    if (guid == uploadGuid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(templatesCount == 1);
            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "playlistlist.json"))))["playlists"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            int playlistsCount = 0;
            foreach (JObject jsonplaylist in jArray.SelectTokens("$.[*]"))
            {
                playlistsCount++;
                string playlistId = jsonplaylist["playlistId"].Value<string>();
                if (playlistId == BasicTests.testPlaylistId)
                {
                    found = true;
                }
            }

            Assert.IsTrue(playlistsCount == 1);
            Assert.IsTrue(found);
        }

        [ConfigSource("BasicTests_RemoveUploadReadyToUploadWithoutTemplate")]
        [Test]
        public static void RemoveUploadReadyToUploadWithTemplate()
        {
            string uploadGuid = "e1957955-c88b-4185-90f5-ce1c9fbfe08d";

            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            UploadListViewModel uploadListViewModel = (UploadListViewModel)mainWindowViewModel.CurrentViewModel;
            uploadListViewModel.DeleteCommand.Execute(uploadGuid);

            Assert.IsTrue(uploadList.UploadCount == 1);
            Upload upload = uploadList.GetUpload(0);
            Assert.IsTrue(upload.Guid != Guid.Parse(uploadGuid));

            Template template = templateList.GetTemplate(0);
            Assert.IsTrue(template.Uploads.Count == 0);

            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 1);

            bool found = false;
            int uploadsCount = 0;
            foreach (JObject jsonUpload in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string guid = jsonUpload["guid"].Value<string>();

                if (guid == uploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            uploadsCount = 0;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string jsonGuid = guid.Value<string>();
                if (jsonGuid == uploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json"))))["templates"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            int templatesCount = 0;
            uploadsCount = 0;
            foreach (JObject jsonTemplate in jArray.SelectTokens("$.[*]"))
            {
                templatesCount++;
                JToken uploads = jsonTemplate["uploads"];
                foreach (JToken jsonUploadGuid in uploads.SelectTokens("$.[*]"))
                {
                    uploadsCount++;
                    string guid = jsonUploadGuid.Value<string>();
                    if (guid == uploadGuid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(templatesCount == 1);
            Assert.IsTrue(uploadsCount == 0);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "playlistlist.json"))))["playlists"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            int playlistsCount = 0;
            foreach (JObject jsonplaylist in jArray.SelectTokens("$.[*]"))
            {
                playlistsCount++;
                string playlistId = jsonplaylist["playlistId"].Value<string>();
                if (playlistId == BasicTests.testPlaylistId)
                {
                    found = true;
                }
            }

            Assert.IsTrue(playlistsCount == 1);
            Assert.IsTrue(found);
        }

        [Test]
        public static void RemoveUploadFinishedWithoutTemplate()
        {
            string uploadGuid = "0f179547-8981-4509-b167-b84d1fdbc2e6";

            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            UploadListViewModel uploadListViewModel = (UploadListViewModel)mainWindowViewModel.CurrentViewModel;
            uploadListViewModel.DeleteCommand.Execute(uploadGuid);

            Assert.IsTrue(uploadList.UploadCount == 1);
            Upload upload = uploadList.GetUpload(0);
            Assert.IsTrue(upload.Guid != Guid.Parse(uploadGuid));

            Template template = templateList.GetTemplate(0);
            Assert.IsTrue(template.Uploads.Count == 1);
            Assert.IsTrue(template.Uploads[0].Guid != Guid.Parse(uploadGuid));

            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 1);

            bool found = false;
            int uploadsCount = 0;
            foreach (JObject jsonUpload in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string guid = jsonUpload["guid"].Value<string>();

                if (guid == uploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            uploadsCount = 0;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string jsonGuid = guid.Value<string>();
                if (jsonGuid == uploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json"))))["templates"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            int templatesCount = 0;
            uploadsCount = 0;
            foreach (JObject jsonTemplate in jArray.SelectTokens("$.[*]"))
            {
                templatesCount++;
                JToken uploads = jsonTemplate["uploads"];
                foreach (JToken jsonUploadGuid in uploads.SelectTokens("$.[*]"))
                {
                    uploadsCount++;
                    string guid = jsonUploadGuid.Value<string>();
                    if (guid == uploadGuid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(templatesCount == 1);
            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "playlistlist.json"))))["playlists"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            int playlistsCount = 0;
            foreach (JObject jsonplaylist in jArray.SelectTokens("$.[*]"))
            {
                playlistsCount++;
                string playlistId = jsonplaylist["playlistId"].Value<string>();
                if (playlistId == BasicTests.testPlaylistId)
                {
                    found = true;
                }
            }

            Assert.IsTrue(playlistsCount == 1);
            Assert.IsTrue(found);
        }

        [ConfigSource("BasicTests_RemoveUploadFinishedWithoutTemplate")]
        [Test]
        public static void RemoveUploadFinishedWithTemplate()
        {
            string uploadGuid = "e1957955-c88b-4185-90f5-ce1c9fbfe08d";

            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            UploadListViewModel uploadListViewModel = (UploadListViewModel)mainWindowViewModel.CurrentViewModel;
            uploadListViewModel.DeleteCommand.Execute(uploadGuid);

            Assert.IsTrue(uploadList.UploadCount == 1);
            Upload upload = uploadList.GetUpload(0);
            Assert.IsTrue(upload.Guid != Guid.Parse(uploadGuid));

            Template template = templateList.GetTemplate(0);
            Assert.IsTrue(template.Uploads.Count == 1);
            Assert.IsTrue(template.Uploads[0].Guid == Guid.Parse(uploadGuid));

            JArray jArray = (JArray)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploads.json")));

            Assert.IsTrue(jArray.Count == 2);

            bool found = false;
            bool foundAgain = false;
            int uploadsCount = 0;
            foreach (JObject jsonUpload in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string guid = jsonUpload["guid"].Value<string>();

                if (guid == uploadGuid)
                {
                    if (found)
                    {
                        foundAgain = true;
                    }

                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 2);
            Assert.IsTrue(found);
            Assert.IsTrue(!foundAgain);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "uploadlist.json"))))["uploads"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            uploadsCount = 0;
            foreach (JValue guid in jArray.SelectTokens("$.[*]"))
            {
                uploadsCount++;
                string jsonGuid = guid.Value<string>();
                if (jsonGuid == uploadGuid)
                {
                    found = true;
                }
            }

            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(!found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "templatelist.json"))))["templates"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            int templatesCount = 0;
            uploadsCount = 0;
            foreach (JObject jsonTemplate in jArray.SelectTokens("$.[*]"))
            {
                templatesCount++;
                JToken uploads = jsonTemplate["uploads"];
                foreach (JToken jsonUploadGuid in uploads.SelectTokens("$.[*]"))
                {
                    uploadsCount++;
                    string guid = jsonUploadGuid.Value<string>();
                    if (guid == uploadGuid)
                    {
                        found = true;
                    }
                }
            }

            Assert.IsTrue(templatesCount == 1);
            Assert.IsTrue(uploadsCount == 1);
            Assert.IsTrue(found);

            jArray = (JArray)((JObject)JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(CurrentSettings.StorageFolder, "playlistlist.json"))))["playlists"];

            Assert.IsTrue(jArray.Count == 1);

            found = false;
            int playlistsCount = 0;
            foreach (JObject jsonplaylist in jArray.SelectTokens("$.[*]"))
            {
                playlistsCount++;
                string playlistId = jsonplaylist["playlistId"].Value<string>();
                if (playlistId == BasicTests.testPlaylistId)
                {
                    found = true;
                }
            }

            Assert.IsTrue(playlistsCount == 1);
            Assert.IsTrue(found);
        }

        [ConfigSource("BasicTests_RemoveUploadFinishedWithoutTemplate")]
        [Test]
        public static void ReferencesAreEqual()
        {
            Guid uploadWithTemplateGuid = Guid.Parse("e1957955-c88b-4185-90f5-ce1c9fbfe08d");
            Guid uploadWithoutTemplateGuid = Guid.Parse("0f179547-8981-4509-b167-b84d1fdbc2e6");

            UploadList uploadList;
            TemplateList templateList;
            PlaylistList playlistList;
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, CurrentSettings.StorageFolder, CurrentSettings.TemplateImageFolder, CurrentSettings.ThumbnailFallbackImageFolder, out uploadList, out templateList, out playlistList);

            UploadListViewModel uploadListViewModel = (UploadListViewModel)mainWindowViewModel.CurrentViewModel;

            mainWindowViewModel.TabNo = 1;
            TemplateViewModel templateViewModel = (TemplateViewModel)mainWindowViewModel.CurrentViewModel;

            mainWindowViewModel.TabNo = 2;
            PlaylistViewModel playlistViewModel = (PlaylistViewModel)mainWindowViewModel.CurrentViewModel;

            Assert.IsTrue(uploadList.UploadCount == 2);
            Assert.IsTrue(templateList.TemplateCount == 1);

            Upload uploadWithTemplate = uploadList.GetUpload(upload => upload.Guid == uploadWithTemplateGuid);
            Upload uploadWithoutTemplate = uploadList.GetUpload(upload => upload.Guid == uploadWithoutTemplateGuid);

            Template template = templateList.GetTemplate(0);

            Playlist playlist = playlistList.GetPlaylist(0);

            Assert.IsTrue(template.Uploads.Count == 1);
            Assert.IsTrue(template.Uploads[0] == uploadWithTemplate);

            Assert.IsTrue(uploadListViewModel.ObservableUploadViewModels.Count() == 2);
            Assert.IsTrue(uploadListViewModel.ObservableUploadViewModels.GetUploadByGuid(uploadWithTemplateGuid).Upload == uploadWithTemplate);
            Assert.IsTrue(uploadListViewModel.ObservableUploadViewModels.GetUploadByGuid(uploadWithoutTemplateGuid).Upload == uploadWithoutTemplate);
            Assert.IsTrue(uploadListViewModel.RemoveTemplateViewModels[2].Template == template);
            Assert.IsTrue(uploadListViewModel.RemoveTemplateViewModels[0].Template == uploadListViewModel.RemoveSelectedTemplate.Template);
            Assert.IsTrue(uploadListViewModel.RemoveTemplateViewModels[0] == uploadListViewModel.RemoveSelectedTemplate);
            Assert.IsTrue(uploadListViewModel.RemoveUploadStatuses[5] == uploadListViewModel.RemoveUploadStatus);

            Assert.IsTrue(templateViewModel.Template == template);
            Assert.IsTrue(templateViewModel.ObservableTemplateViewModels[0].Template == template);
            Assert.IsTrue(templateViewModel.ObservablePlaylistViewModels[0].Playlist == playlist);
            Assert.IsTrue(templateViewModel.ObservableTemplateViewModels[0] == templateViewModel.SelectedTemplate);
            Assert.IsTrue(templateViewModel.SelectedTemplate.Template == template);
            Assert.IsTrue(templateViewModel.ObservablePlaylistViewModels[0] == templateViewModel.SelectedPlaylist);

            Assert.IsTrue(playlistViewModel.Playlist == playlist);
            Assert.IsTrue(playlistViewModel.ObservablePlaylistViewModels[0].Playlist == playlist);
            Assert.IsTrue(playlistViewModel.ObservablePlaylistViewModels[0] == playlistViewModel.SelectedPlaylist);
        }

        //todo: add tests for editing upload/template/playlist
    }
}
