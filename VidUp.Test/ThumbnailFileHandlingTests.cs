#region

using System;
using System.Collections.Generic;
using System.IO;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.ViewModels;
using NUnit.Framework;

#endregion

namespace Drexel.VidUp.Test
{
  
    public class ThumbnailFileHandlingTests
    {
        private static TemplateViewModel templateViewModel;
        private static UploadListViewModel uploadListViewModel;

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
            
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix,null, out _, out ThumbnailFileHandlingTests.templateList, out _);
            ThumbnailFileHandlingTests.uploadListViewModel = (UploadListViewModel)mainWindowViewModel.CurrentViewModel;
            mainWindowViewModel.TabNo = 1;
            ThumbnailFileHandlingTests.templateViewModel = (TemplateViewModel)mainWindowViewModel.CurrentViewModel;

            Directory.CreateDirectory(ThumbnailFileHandlingTests.t1RootFolder);
            Directory.CreateDirectory(Path.Combine(ThumbnailFileHandlingTests.t1RootFolder, "videos"));
            File.Copy(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestAssets", "video1.mkv"), ThumbnailFileHandlingTests.t1RootVideo1FilePath);

            ThumbnailFileHandlingTests.t1 = new Template("T1", null, TemplateMode.FolderBased, ThumbnailFileHandlingTests.t1RootFolder, null, ThumbnailFileHandlingTests.templateList);
            ThumbnailFileHandlingTests.t2 = new Template("T2", null, TemplateMode.FolderBased, null, null, ThumbnailFileHandlingTests.templateList);
            ThumbnailFileHandlingTests.t3 = new Template("T3", null, TemplateMode.FolderBased, null, null, ThumbnailFileHandlingTests.templateList);

            ThumbnailFileHandlingTests.templateViewModel.AddTemplate(t1);
            ThumbnailFileHandlingTests.templateViewModel.AddTemplate(t2);
            ThumbnailFileHandlingTests.templateViewModel.AddTemplate(t3);
        }

        [OneTimeTearDown]
        public static void CleanUp()
        {
            ThumbnailFileHandlingTests.templateViewModel = null;
            ThumbnailFileHandlingTests.uploadListViewModel = null;

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
            List<Upload> uploads = new List<Upload>();
            ThumbnailFileHandlingTests.u1 = new Upload(ThumbnailFileHandlingTests.t1RootVideo1FilePath);
            uploads.Add(ThumbnailFileHandlingTests.u1);

            ThumbnailFileHandlingTests.uploadListViewModel.AddUploads(uploads);
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
            ThumbnailFileHandlingTests.templateViewModel.DeleteTemplate(ThumbnailFileHandlingTests.t1.Guid.ToString());
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(17)]
        public void TestRemoveU1()
        {
            ThumbnailFileHandlingTests.uploadListViewModel.RemoveUpload(ThumbnailFileHandlingTests.u1.Guid.ToString());
            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(18)]
        public void ReAddT1AndU1()
        {
            ThumbnailFileHandlingTests.t1 = new Template("T1", null, TemplateMode.FolderBased, ThumbnailFileHandlingTests.t1RootFolder, null, templateList);
            ThumbnailFileHandlingTests.templateViewModel.AddTemplate(t1);

            ThumbnailFileHandlingTests.t1.ThumbnailFallbackFilePath = ThumbnailFileHandlingTests.thumbNailFallbackImage1SourceFilePath;
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));

            List<Upload> uploads = new List<Upload>();
            ThumbnailFileHandlingTests.u1 = new Upload(ThumbnailFileHandlingTests.t1RootVideo1FilePath);
            uploads.Add(ThumbnailFileHandlingTests.u1);

            ThumbnailFileHandlingTests.uploadListViewModel.AddUploads(uploads);
            Assert.IsTrue(ThumbnailFileHandlingTests.u1.ThumbnailFilePath == ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath);
        }

        [Test, Order(19)]
        public void TestRemoveU12()
        {
            ThumbnailFileHandlingTests.uploadListViewModel.RemoveUpload(ThumbnailFileHandlingTests.u1.Guid.ToString());
            Assert.IsTrue(File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }

        [Test, Order(20)]
        public void TestRemoveT12()
        {
            ThumbnailFileHandlingTests.templateViewModel.DeleteTemplate(ThumbnailFileHandlingTests.t1.Guid.ToString());
            Assert.IsTrue(!File.Exists(ThumbnailFileHandlingTests.thumbNailFallbackImage1TargetFilePath));
        }
    }
}
