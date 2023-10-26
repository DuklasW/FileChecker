using NUnit.Framework;
using FileWatcherLibrary;
using System;
using System.IO;

namespace YourUnitTestsNamespace
{
    [TestFixture]
    public class FileWatcherManagerTests
    {
        private string _testFolderPath;

        [SetUp]
        public void Setup()
        {
            _testFolderPath = Path.Combine(Path.GetTempPath(), "TestFolder");
            Directory.CreateDirectory(_testFolderPath);
        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(_testFolderPath, true);
        }

        [Test]
        public void FileWatcherManager_CanBeCreated()
        {
            // Arrange
            var fileWatcherManager = new FileWatcherManager(_testFolderPath);

            // Assert
            Assert.IsNotNull(fileWatcherManager);
        }

        [Test]
        public void FileWatcherManager_StartAndStopWatchingFolder()
        {
            // Arrange
            var fileWatcherManager = new FileWatcherManager(_testFolderPath);

            // Act
            fileWatcherManager.StartFileWatch();
            fileWatcherManager.StopFileWatch();

            // Assert
            Assert.IsFalse(fileWatcherManager._fileSystemWatcher.EnableRaisingEvents);
        }

        [Test]
        public void FileWatcherManager_ThrowsExceptionWhenPathDoesNotExist()
        {
            // Arrange
            var invalidPath = "InvalidPath";

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => new FileWatcherManager(invalidPath));
        }

        [Test]
        public void FileWatcherManager_ChangePathUpdatesPath()
        {
            // Arrange
            var fileWatcherManager = new FileWatcherManager(_testFolderPath);
            var newPath = Path.Combine(Path.GetTempPath(), "NewTestFolder");
            Directory.CreateDirectory(newPath);

            // Act
            fileWatcherManager.ChangePath(newPath);

            // Assert
            Assert.That(fileWatcherManager._fileSystemWatcher.Path, Is.EqualTo(newPath));
        }
    }
}
