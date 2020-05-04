using System;
using System.Collections.Generic;
using System.IO;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Drexel.VidUp.Test
{
    [TestClass]
    public class ThumbnailFileHandlingTests
    {
        private static MainWindowViewModel mainWindowViewModel;
        private static string t1RootFolder = Path.Combine(Directory.GetCurrentDirectory(), "T1Root");
        private static string t1RootVideo1FilePath = Path.Combine(ThumbnailFileHandlingTests.t1RootFolder, "videos", "video1.mkv");
        private static string thumbNailFallbackImage1SourceFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", "image1.png");
        private static string thumbNailFallbackImage1TargetFilePath = Path.Combine(Settings.ThumbnailFallbackImageFolder, "image1.png");
        private static string thumbNailFallbackFileExistedImage12TargetFilePath = Path.Combine(Settings.ThumbnailFallbackImageFolder, "image12.png");
        private static string thumbNailSourceImage2FilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", "image2.png");

        private static Template t1;
        private static Template t2;
        private static Template t3;

        private static Upload u1;

        private static TemplateList templateList;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Directory.Exists(Settings.StorageFolder))
            {
                Directory.Delete(Settings.StorageFolder, true);
            }

            if (Directory.Exists(ThumbnailFileHandlingTests.t1RootFolder))
            {
                Directory.Delete(ThumbnailFileHandlingTests.t1RootFolder, true);
            }

            ThumbnailFileHandlingTests.mainWindowViewModel = new MainWindowViewModel(Settings.UserSuffix, Settings.StorageFolder, Settings.TemplateImageFolder, Settings.ThumbnailFallbackImageFolder, out ThumbnailFileHandlingTests.templateList);

            Directory.CreateDirectory(ThumbnailFileHandlingTests.t1RootFolder);
            Directory.CreateDirectory(Path.Combine(ThumbnailFileHandlingTests.t1RootFolder, "videos"));
            File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", "video1.mkv"), ThumbnailFileHandlingTests.t1RootVideo1FilePath);

            ThumbnailFileHandlingTests.t1 = new Template("T1", null, ThumbnailFileHandlingTests.t1RootFolder, ThumbnailFileHandlingTests.templateList);
            ThumbnailFileHandlingTests.t2 = new Template("T2", null, null, ThumbnailFileHandlingTests.templateList);
            ThumbnailFileHandlingTests.t3 = new Template("T3", null, null, ThumbnailFileHandlingTests.templateList);

            ThumbnailFileHandlingTests.mainWindowViewModel.AddTemplate(t1);
            ThumbnailFileHandlingTests.mainWindowViewModel.AddTemplate(t2);
            ThumbnailFileHandlingTests.mainWindowViewModel.AddTemplate(t3);
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            mainWindowViewModel = null;
            Directory.Delete(Settings.StorageFolder, true);
            Directory.Delete(ThumbnailFileHandlingTests.t1RootFolder, true);
        }

        [TestMethod]
        public void T001_TestAddT1FallBackThumbnail()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }
        
        [TestMethod]
        public void T002_TestAddT2FallBackThumbnailAgain()
        {
            ThumbnailFileHandlingTests.t2.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackFileExistedImage12TargetFilePath));
        }

        [TestMethod]
        public void T003_TestAddU1FromT1RootFolderAutoSetFallBackThumbnail()
        {
            List<Upload> uploads = new List<Upload>();
            ThumbnailFileHandlingTests.u1 = new Upload(ThumbnailFileHandlingTests.t1RootVideo1FilePath);
            uploads.Add(ThumbnailFileHandlingTests.u1);

            ThumbnailFileHandlingTests.mainWindowViewModel.AddUploads(uploads);
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
        }

        [TestMethod]
        public void T004_TestRemoveU1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == null);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T005_TestAddU1InidividualThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = ThumbnailFileHandlingTests.thumbNailSourceImage2FilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailSourceImage2FilePath);
            Assert.IsTrue(!File.Exists(Path.Combine(Settings.ThumbnailFallbackImageFolder, "image2.png")));
        }

        [TestMethod]
        public void T006_TestRemoveU1InidividualThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == null);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailSourceImage2FilePath));
        }

        [TestMethod]
        public void T007_TestAddU1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
            Assert.IsTrue(!File.Exists(Path.Combine(Settings.ThumbnailFallbackImageFolder, "image13.png")));            
        }

        [TestMethod]
        public void T008_TestRemoveT1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath == null);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T009_TestRemoveU1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == null);
            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T010_TestAddT1FallBackThumbnail()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T011_TestAddU1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
        }

        [TestMethod]
        public void T012_TestRemoveU1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == null);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T013_TestRemoveT1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath == null);
            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T014_TestAddT1FallBackThumbnail()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T015_TestAddU1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
        }

        [TestMethod]
        public void T016_TestRemoveT1()
        {
            ThumbnailFileHandlingTests.mainWindowViewModel.RemoveTemplate(ThumbnailFileHandlingTests.t1.Guid.ToString());
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T017_TestRemoveU1()
        {
            ThumbnailFileHandlingTests.mainWindowViewModel.RemoveUpload(ThumbnailFileHandlingTests.u1.Guid.ToString());
            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T018_ReAddT1AndU1()
        {
            ThumbnailFileHandlingTests.t1 = new Template("T1", null, ThumbnailFileHandlingTests.t1RootFolder, templateList);
            ThumbnailFileHandlingTests.mainWindowViewModel.AddTemplate(t1);

            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));

            List<Upload> uploads = new List<Upload>();
            ThumbnailFileHandlingTests.u1 = new Upload(ThumbnailFileHandlingTests.t1RootVideo1FilePath);
            uploads.Add(ThumbnailFileHandlingTests.u1);

            ThumbnailFileHandlingTests.mainWindowViewModel.AddUploads(uploads);
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
        }

        [TestMethod]
        public void T019_TestRemoveU1()
        {
            ThumbnailFileHandlingTests.mainWindowViewModel.RemoveUpload(ThumbnailFileHandlingTests.u1.Guid.ToString());
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [TestMethod]
        public void T020_TestRemoveT1()
        {
            ThumbnailFileHandlingTests.mainWindowViewModel.RemoveTemplate(ThumbnailFileHandlingTests.t1.Guid.ToString());
            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }
    }
}
