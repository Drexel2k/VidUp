using System;
using System.IO;
using Drexel.VidUp.UI;
using Drexel.VidUp.UI.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Vidup.Test
{
    [TestClass]
    public class FileHandlingTests
    {
        private static MainWindowViewModel mainWindowViewModel;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            FileHandlingTests.mainWindowViewModel = new MainWindowViewModel(Settings.UserSuffix, Settings.StorageFolder, Settings.TemplateImageFolder, Settings.ThumbnailFallbackImageFolder);
        }

        [ClassCleanup]
        public void CleanUp()
        {
            FileHandlingTests.mainWindowViewModel = null;
            Directory.Delete(Settings.StorageFolder, true);
        }

        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsTrue(true);
        }
    }
}
