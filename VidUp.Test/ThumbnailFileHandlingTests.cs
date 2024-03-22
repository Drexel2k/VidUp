using System;
using System.Collections.Generic;
using System.IO;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.ViewModels;
using NUnit.Framework;

using Assert = NUnit.Framework.Legacy.ClassicAssert;
/* until overwork to
 * Assert.That(e.Text == "Forgot your password?", "Verified forgotten password link text.");
Assert.That(e.Text == "Forgot your password?", Is.True,"Verified forgotten password link text."
Assert.That(e.Text,Is.EqualTo("Forgot your password?"), "Verified forgotten password link text.");
*/

namespace Drexel.VidUp.Test
{
  
    public class ThumbnailFileHandlingTests
    {
        private static TemplateRibbonViewModel templateRibbonViewModel;
        private static SettingsRibbonViewModel settingsRibbonViewModel;
        private static TemplateViewModel templateViewModel;
        private static UploadRibbonViewModel uploadRibbonViewModel;

        private static string t1RootFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "T1Root");
        private static string t1RootVideo1FilePath = Path.Combine(ThumbnailFileHandlingTests.t1RootFolder, "videos", "video1.mkv");
        private static string thumbNailFallbackImage1SourceFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestAssets", "image1.png");
        private static string thumbNailFallbackImage1TargetFilePath;
        private static string thumbNailFallbackFileExistedImage12TargetFilePath;
        private static string thumbNailSourceImage2FilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestAssets", "image2.png");

        private static Template t1;
        private static Template t2;
        private static Template t3;

        private static Upload u1;

        private static TemplateList templateList;
        private static MainWindowViewModel mainWindowViewModel;
        private static UploadListViewModel uploadListViewModel;

        [OneTimeSetUp]
        public static void Initialize()
        {
            if (Directory.Exists(BaseSettings.StorageFolder))
            {
                Directory.Delete(BaseSettings.StorageFolder, true);
            }

            if (Directory.Exists(ThumbnailFileHandlingTests.t1RootFolder))
            {
                Directory.Delete(ThumbnailFileHandlingTests.t1RootFolder, true);
            }

            BaseSettings.SubFolder = String.Empty;
            ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath = Path.Combine(BaseSettings.ThumbnailFallbackImageFolder, "image1.png");
            ThumbnailFileHandlingTests.thumbNailFallbackFileExistedImage12TargetFilePath = Path.Combine(BaseSettings.ThumbnailFallbackImageFolder, "image12.png");

            List<object> ribbonViewModels;
            ThumbnailFileHandlingTests.mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix,null, out _, out ThumbnailFileHandlingTests.templateList, out _, out ribbonViewModels);
            ThumbnailFileHandlingTests.uploadListViewModel = (UploadListViewModel)ThumbnailFileHandlingTests.mainWindowViewModel.CurrentViewModel;
            ThumbnailFileHandlingTests.mainWindowViewModel.TabNo = 1;
            ThumbnailFileHandlingTests.templateViewModel = (TemplateViewModel)ThumbnailFileHandlingTests.mainWindowViewModel.CurrentViewModel;
            ThumbnailFileHandlingTests.uploadRibbonViewModel = (UploadRibbonViewModel)ribbonViewModels[0];
            ThumbnailFileHandlingTests.templateRibbonViewModel = (TemplateRibbonViewModel)ribbonViewModels[1];
            ThumbnailFileHandlingTests.settingsRibbonViewModel = (SettingsRibbonViewModel)ribbonViewModels[3];

            Directory.CreateDirectory(ThumbnailFileHandlingTests.t1RootFolder);
            Directory.CreateDirectory(Path.Combine(ThumbnailFileHandlingTests.t1RootFolder, "videos"));
            File.Copy(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestAssets", "video1.mkv"), ThumbnailFileHandlingTests.t1RootVideo1FilePath);

            ThumbnailFileHandlingTests.t1 = new Template("T1", null, TemplateMode.FolderBased, ThumbnailFileHandlingTests.t1RootFolder, null, ThumbnailFileHandlingTests.templateList, ((SettingsRibbonViewModel)ribbonViewModels[3]).ObservableYoutubeAccountViewModels[0].YoutubeAccount);
            ThumbnailFileHandlingTests.t2 = new Template("T2", null, TemplateMode.FolderBased, null, null, ThumbnailFileHandlingTests.templateList, ((SettingsRibbonViewModel)ribbonViewModels[3]).ObservableYoutubeAccountViewModels[0].YoutubeAccount);
            ThumbnailFileHandlingTests.t3 = new Template("T3", null, TemplateMode.FolderBased, null, null, ThumbnailFileHandlingTests.templateList, ((SettingsRibbonViewModel)ribbonViewModels[3]).ObservableYoutubeAccountViewModels[0].YoutubeAccount);

            ThumbnailFileHandlingTests.templateRibbonViewModel.AddTemplate(t1);
            ThumbnailFileHandlingTests.templateRibbonViewModel.AddTemplate(t2);
            ThumbnailFileHandlingTests.templateRibbonViewModel.AddTemplate(t3);
        }

        [OneTimeTearDown]
        public static void CleanUp()
        {
            ThumbnailFileHandlingTests.templateRibbonViewModel = null;
            ThumbnailFileHandlingTests.uploadRibbonViewModel = null;
            ThumbnailFileHandlingTests.mainWindowViewModel.Close();
            ThumbnailFileHandlingTests.mainWindowViewModel = null;

            if (Directory.Exists(BaseSettings.StorageFolder))
            {
                Directory.Delete(BaseSettings.StorageFolder, true);
            }

            if (Directory.Exists(ThumbnailFileHandlingTests.t1RootFolder))
            {
                Directory.Delete(ThumbnailFileHandlingTests.t1RootFolder, true);
            }
        }

        [Test, Order(1)]
        public void TestAddT1FallBackThumbnail()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(2)]
        public void TestAddT2FallBackThumbnailAgain()
        {
            ThumbnailFileHandlingTests.t2.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackFileExistedImage12TargetFilePath));
        }

        [Test, Order(3)]
        public void TestAddU1FromT1RootFolderAutoSetFallBackThumbnail()
        {
            List<string> files = new List<string>();
            files.Add(ThumbnailFileHandlingTests.t1RootVideo1FilePath);
            ThumbnailFileHandlingTests.uploadRibbonViewModel.AddFiles(files.ToArray(), false);

            ThumbnailFileHandlingTests.u1 = ThumbnailFileHandlingTests.uploadRibbonViewModel.ObservableUploadViewModels[0].Upload;

            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
        }

        [Test, Order(4)]
        public void TestRemoveU1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == null);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(5)]
        public void TestAddU1InidividualThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = ThumbnailFileHandlingTests.thumbNailSourceImage2FilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailSourceImage2FilePath);
            Assert.IsTrue(!File.Exists(Path.Combine(BaseSettings.ThumbnailFallbackImageFolder, "image2.png")));
        }

        [Test, Order(6)]
        public void TestRemoveU1InidividualThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == null);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailSourceImage2FilePath));
        }

        [Test, Order(7)]
        public void TestAddU1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
            Assert.IsTrue(!File.Exists(Path.Combine(BaseSettings.ThumbnailFallbackImageFolder, "image13.png")));            
        }

        [Test, Order(8)]
        public void TestRemoveT1FallbackThumbnail()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath == null);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(9)]
        public void TestRemoveU1FallbackThumbnail2()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == null);
            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(10)]
        public void TestAddT1FallBackThumbnail2()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(11)]
        public void TestAddU1FallbackThumbnail2()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
        }

        [Test, Order(12)]
        public void TestRemoveU1FallbackThumbnail3()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == null);
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(13)]
        public void TestRemoveT1FallbackThumbnail2()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = null;
            Assert.IsTrue(ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath == null);
            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(14)]
        public void TestAddT1FallBackThumbnail3()
        {
            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(15)]
        public void TestAddU1FallbackThumbnail3()
        {
            ThumbnailFileHandlingTests.u1.ThumbnailFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath;
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
        }

        [Test, Order(16)]
        public void TestRemoveT1()
        {
            ThumbnailFileHandlingTests.templateRibbonViewModel.SelectedTemplate = ThumbnailFileHandlingTests.templateRibbonViewModel.ObservableTemplateViewModels.GetViewModel(ThumbnailFileHandlingTests.t1);
            ThumbnailFileHandlingTests.mainWindowViewModel.TabNo = 1;
            TemplateViewModel templateViewModel = (TemplateViewModel)ThumbnailFileHandlingTests.mainWindowViewModel.CurrentViewModel;
            templateViewModel.ParameterlessCommandAction("delete");

            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(17)]
        public void TestRemoveU1()
        {
            ThumbnailFileHandlingTests.mainWindowViewModel.TabNo = 0;
            UploadListViewModel uploadListViewModel = (UploadListViewModel)ThumbnailFileHandlingTests.mainWindowViewModel.CurrentViewModel;
            uploadListViewModel.DeleteUpload(ThumbnailFileHandlingTests.u1.Guid.ToString());

            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(18)]
        public void ReAddT1AndU1()
        {
            ThumbnailFileHandlingTests.t1 = new Template("T1", null, TemplateMode.FolderBased, ThumbnailFileHandlingTests.t1RootFolder, null, templateList, ThumbnailFileHandlingTests.settingsRibbonViewModel.ObservableYoutubeAccountViewModels[0].YoutubeAccount);
            ThumbnailFileHandlingTests.templateRibbonViewModel.AddTemplate(t1);

            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));

            List<string> files = new List<string>();
            files.Add(ThumbnailFileHandlingTests.t1RootVideo1FilePath);
            ThumbnailFileHandlingTests.uploadRibbonViewModel.AddFiles(files.ToArray(), false);

            ThumbnailFileHandlingTests.u1 = ThumbnailFileHandlingTests.uploadRibbonViewModel.ObservableUploadViewModels[0].Upload;

            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
        }

        [Test, Order(19)]
        public void TestRemoveU12()
        {
            ThumbnailFileHandlingTests.uploadListViewModel.DeleteUpload(ThumbnailFileHandlingTests.u1.Guid.ToString());
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(20)]
        public void TestRemoveT12()
        {
            ThumbnailFileHandlingTests.templateRibbonViewModel.SelectedTemplate = ThumbnailFileHandlingTests.templateRibbonViewModel.ObservableTemplateViewModels.GetViewModel(ThumbnailFileHandlingTests.t1);
            ThumbnailFileHandlingTests.templateViewModel.ParameterlessCommandAction("delete");
            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }
    }
}
