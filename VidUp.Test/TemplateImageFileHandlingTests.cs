using System.IO;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.ViewModels;
using NUnit.Framework;
using System.Collections.Generic;

using Assert = NUnit.Framework.Legacy.ClassicAssert;
/* until overwork to
 * Assert.That(e.Text == "Forgot your password?", "Verified forgotten password link text.");
Assert.That(e.Text == "Forgot your password?", Is.True,"Verified forgotten password link text."
Assert.That(e.Text,Is.EqualTo("Forgot your password?"), "Verified forgotten password link text.");
*/

namespace Drexel.VidUp.Test
{
    public class TemplateImageFileHandlingTests
    {
        private static MainWindowViewModel mainWindowViewModel;
        private static TemplateRibbonViewModel templateRibbonViewModel;
        private static string t1RootFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "T1Root");
        private static string templateImage1SourceFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestAssets", "image1.png");
        private static string templateImage1TargetFilePath;
        private static string templateImageFileExistedImage12TargetFilePath;

        private static Template t1;
        private static Template t2;
        private static Template t3;

        private static TemplateList templateList;

        [OneTimeSetUp]
        public static void Initialize()
        {
            if (Directory.Exists(BaseSettings.StorageFolder))
            {
                Directory.Delete(BaseSettings.StorageFolder, true);
            }

            if (Directory.Exists(TemplateImageFileHandlingTests.t1RootFolder))
            {
                Directory.Delete(TemplateImageFileHandlingTests.t1RootFolder, true);
            }

            Directory.CreateDirectory(TemplateImageFileHandlingTests.t1RootFolder);
            Directory.CreateDirectory(Path.Combine(TemplateImageFileHandlingTests.t1RootFolder, "videos"));

            BaseSettings.SubFolder = string.Empty;
            TemplateImageFileHandlingTests.templateImage1TargetFilePath = Path.Combine(BaseSettings.TemplateImageFolder, "image1.png");
            TemplateImageFileHandlingTests.templateImageFileExistedImage12TargetFilePath = Path.Combine(BaseSettings.TemplateImageFolder, "image12.png");

            List<object> ribbonViewModels;
            TemplateImageFileHandlingTests.mainWindowViewModel = new MainWindowViewModel(BaseSettings.UserSuffix, null, out _, out TemplateImageFileHandlingTests.templateList, out _, out ribbonViewModels);
            TemplateImageFileHandlingTests.templateRibbonViewModel = (TemplateRibbonViewModel)ribbonViewModels[1];

            TemplateImageFileHandlingTests.t1 = new Template("T1", null, TemplateMode.FolderBased, TemplateImageFileHandlingTests.t1RootFolder, null, TemplateImageFileHandlingTests.templateList, ((SettingsRibbonViewModel)ribbonViewModels[3]).ObservableYoutubeAccountViewModels[0].YoutubeAccount);
            TemplateImageFileHandlingTests.t2 = new Template("T2", null, TemplateMode.FolderBased, null, null, TemplateImageFileHandlingTests.templateList, ((SettingsRibbonViewModel)ribbonViewModels[3]).ObservableYoutubeAccountViewModels[0].YoutubeAccount);
            TemplateImageFileHandlingTests.t3 = new Template("T3", null, TemplateMode.FolderBased, null, null, TemplateImageFileHandlingTests.templateList, ((SettingsRibbonViewModel)ribbonViewModels[3]).ObservableYoutubeAccountViewModels[0].YoutubeAccount);

            TemplateImageFileHandlingTests.templateRibbonViewModel.AddTemplate(t1);
            TemplateImageFileHandlingTests.templateRibbonViewModel.AddTemplate(t2);
            TemplateImageFileHandlingTests.templateRibbonViewModel.AddTemplate(t3);
        }

        [OneTimeTearDown]
        public static void CleanUp()
        {
            TemplateImageFileHandlingTests.mainWindowViewModel.Close();
            TemplateImageFileHandlingTests.mainWindowViewModel = null;
            TemplateImageFileHandlingTests.templateRibbonViewModel = null;
            TemplateImageFileHandlingTests.t1RootFolder = null;
            TemplateImageFileHandlingTests.templateImage1SourceFilePath = null;
            TemplateImageFileHandlingTests.templateImage1TargetFilePath = null;
            TemplateImageFileHandlingTests.templateImageFileExistedImage12TargetFilePath = null;

            TemplateImageFileHandlingTests.t1 = null;
            TemplateImageFileHandlingTests.t2 = null;
            TemplateImageFileHandlingTests.t3 = null;

            TemplateImageFileHandlingTests.templateList = null;


            if (Directory.Exists(BaseSettings.StorageFolder))
            {
                Directory.Delete(BaseSettings.StorageFolder, true);
            }

            if (Directory.Exists(TemplateImageFileHandlingTests.t1RootFolder))
            {
                Directory.Delete(TemplateImageFileHandlingTests.t1RootFolder, true);
            }
        }

        [Test, Order(1)]
        public static void TestAddT1TemplateImage()
        {
            TemplateImageFileHandlingTests.t1.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1SourceFilePath;
            Assert.IsTrue(TemplateImageFileHandlingTests.t1.ImageFilePathForEditing == TemplateImageFileHandlingTests.templateImage1TargetFilePath);
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }

        [Test, Order(2)]
        public static void TestAddT2TemplateImageAgain()
        {
            TemplateImageFileHandlingTests.t2.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1SourceFilePath;
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImageFileExistedImage12TargetFilePath));
        }

        [Test, Order(3)]
        public static void TestAddT2TemplateImageFromTemplateImageStorageFolder()
        {
            TemplateImageFileHandlingTests.t2.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1TargetFilePath;
            Assert.IsTrue(TemplateImageFileHandlingTests.t2.ImageFilePathForEditing == TemplateImageFileHandlingTests.templateImage1TargetFilePath);
            Assert.IsTrue(!File.Exists(Path.Combine(BaseSettings.TemplateImageFolder, "image13.png")));
        }

        [Test, Order(4)]
        public static void TestRemoveT1TemplateImage()
        {
            TemplateImageFileHandlingTests.t1.ImageFilePathForEditing = null;
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }

        [Test, Order(5)]
        public static void TestRemoveT2TemplateImage()
        {
            TemplateImageFileHandlingTests.t2.ImageFilePathForEditing = null;
            Assert.IsTrue(!File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }

        [Test, Order(6)]
        public static void TestAddT1T2TemplateImage()
        {
            TemplateImageFileHandlingTests.t1.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1SourceFilePath;
            Assert.IsTrue(TemplateImageFileHandlingTests.t1.ImageFilePathForEditing == TemplateImageFileHandlingTests.templateImage1TargetFilePath);
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));

            TemplateImageFileHandlingTests.t2.ImageFilePathForEditing = TemplateImageFileHandlingTests.templateImage1TargetFilePath;
            Assert.IsTrue(TemplateImageFileHandlingTests.t2.ImageFilePathForEditing == TemplateImageFileHandlingTests.templateImage1TargetFilePath);
        }

        [Test, Order(7)]
        public static void TestRemoveT1()
        {
            TemplateImageFileHandlingTests.templateRibbonViewModel.SelectedTemplate = TemplateImageFileHandlingTests.templateRibbonViewModel.ObservableTemplateViewModels.GetViewModel(TemplateImageFileHandlingTests.t1);
            TemplateImageFileHandlingTests.mainWindowViewModel.TabNo = 1;
            TemplateViewModel templateViewModel = (TemplateViewModel)TemplateImageFileHandlingTests.mainWindowViewModel.CurrentViewModel;
            templateViewModel.ParameterlessCommandAction("delete");
            Assert.IsTrue(File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }

        [Test, Order(8)]
        public static void TestRemoveT2()
        {
            TemplateImageFileHandlingTests.templateRibbonViewModel.SelectedTemplate = TemplateImageFileHandlingTests.templateRibbonViewModel.ObservableTemplateViewModels.GetViewModel(TemplateImageFileHandlingTests.t2);
            TemplateImageFileHandlingTests.mainWindowViewModel.TabNo = 1;
            TemplateViewModel templateViewModel = (TemplateViewModel)TemplateImageFileHandlingTests.mainWindowViewModel.CurrentViewModel;
            templateViewModel.ParameterlessCommandAction("delete");
            Assert.IsTrue(!File.Exists(TemplateImageFileHandlingTests.templateImage1TargetFilePath));
        }
    }
}
