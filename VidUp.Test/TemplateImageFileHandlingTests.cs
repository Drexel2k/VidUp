using System;
using System.IO;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Drexel.VidUp.Test
{
    [TestClass]
    public class TemplateImageFileHandlingTests
    {
        private static MainWindowViewModel mainWindowViewModel;
        private static string t1RootFolder = Path.Combine(Directory.GetCurrentDirectory(), "T1Root");
        private static string templateImage1SourceFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", "image1.png");
        private static string templateImage1TargetFilePath = Path.Combine(Settings.TemplateImageFolder, "image1.png");
        private static string templateImageFileExistedImage12TargetFilePath = Path.Combine(Settings.TemplateImageFolder, "image12.png");

        private static Template t1;
        private static Template t2;
        private static Template t3;

        private static TemplateList templateList;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Directory.Exists(Settings.StorageFolder))
            {
                Directory.Delete(Settings.StorageFolder, true);
            }

            if (Directory.Exists(TemplateImageFileHandlingTests.t1RootFolder))
            {
                Directory.Delete(TemplateImageFileHandlingTests.t1RootFolder, true);
            }

            TemplateImageFileHandlingTests.mainWindowViewModel = new MainWindowViewModel(Settings.UserSuffix, Settings.StorageFolder, Settings.TemplateImageFolder, Settings.ThumbnailFallbackImageFolder, out TemplateImageFileHandlingTests.templateList);

            Directory.CreateDirectory(TemplateImageFileHandlingTests.t1RootFolder);
            Directory.CreateDirectory(Path.Combine(TemplateImageFileHandlingTests.t1RootFolder, "videos"));

            TemplateImageFileHandlingTests.t1 = new Template("T1", null, TemplateImageFileHandlingTests.t1RootFolder, TemplateImageFileHandlingTests.templateList);
            TemplateImageFileHandlingTests.t2 = new Template("T2", null, null, TemplateImageFileHandlingTests.templateList);
            TemplateImageFileHandlingTests.t3 = new Template("T3", null, null, TemplateImageFileHandlingTests.templateList);

            TemplateImageFileHandlingTests.mainWindowViewModel.AddTemplate(t1);
            TemplateImageFileHandlingTests.mainWindowViewModel.AddTemplate(t2);
            TemplateImageFileHandlingTests.mainWindowViewModel.AddTemplate(t3);
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            mainWindowViewModel = null;
            Directory.Delete(Settings.StorageFolder, true);
            Directory.Delete(TemplateImageFileHandlingTests.t1RootFolder, true);
        }

        [TestMethod]
        public void T001_TestAddT1TemplateImage()
        {
            TemplateImageFileHandlingTests.t1.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1SourceFilePath;
            Assert.IsTrue(TemplateImageFileHandlingTests.t1.ImageFilePathForEditing == TemplateImageFileHandlingTests.templateImage1TargetFilePath);
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }

        [TestMethod]
        public void T002_TestAddT2TemplateImageAgain()
        {
            TemplateImageFileHandlingTests.t2.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1SourceFilePath;
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImageFileExistedImage12TargetFilePath));
        }

        [TestMethod]
        public void T003_TestAddT2TemplateImageFromTemplateImageStorageFolder()
        {
            TemplateImageFileHandlingTests.t2.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1TargetFilePath;
            Assert.IsTrue(TemplateImageFileHandlingTests.t2.ImageFilePathForEditing == TemplateImageFileHandlingTests.templateImage1TargetFilePath);
            Assert.IsTrue(!File.Exists(Path.Combine(Settings.TemplateImageFolder, "image13.png")));
        }

        [TestMethod]
        public void T004_TestRemoveT1TemplateImag()
        {
            TemplateImageFileHandlingTests.t1.ImageFilePathForEditing = null;
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }

        [TestMethod]
        public void T005_TestRemoveT2TemplateImage()
        {
            TemplateImageFileHandlingTests.t2.ImageFilePathForEditing = null;
            Assert.IsTrue(!File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }

        [TestMethod]
        public void T006_TestAddT1T2TemplateImage()
        {
            TemplateImageFileHandlingTests.t1.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1SourceFilePath;
            Assert.IsTrue(TemplateImageFileHandlingTests.t1.ImageFilePathForEditing == TemplateImageFileHandlingTests.templateImage1TargetFilePath);
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));

            TemplateImageFileHandlingTests.t2.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1TargetFilePath;
            Assert.IsTrue(TemplateImageFileHandlingTests.t2.ImageFilePathForEditing == TemplateImageFileHandlingTests.templateImage1TargetFilePath);
        }

        [TestMethod]
        public void T007_TestRemoveT1()
        {
            TemplateImageFileHandlingTests.mainWindowViewModel.RemoveTemplate(TemplateImageFileHandlingTests.t1.Guid.ToString());
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }

        [TestMethod]
        public void T008_TestRemoveT2()
        {
            TemplateImageFileHandlingTests.mainWindowViewModel.RemoveTemplate(TemplateImageFileHandlingTests.t2.Guid.ToString());
            Assert.IsTrue(!File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }
    }
}
