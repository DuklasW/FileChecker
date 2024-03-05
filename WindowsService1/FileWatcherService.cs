using System;
using System.Collections.Generic;
using FileWatcherLibrary;

namespace WindowsService1
{

    public class FileWatcherService
    {
        private static FileWatcherManager _fileWatcherManager;
        private static string _currentFolderPath;
        private static string _currentFilePath;

        //zmieniłem z GetFileWatcherManager
        public static FileWatcherManager GetDirectoryWatcherManager(string path)
        {
            if (_fileWatcherManager == null || !path.Equals(_currentFolderPath, System.StringComparison.OrdinalIgnoreCase))
            {
                StopDirectoryWatcher();

                _fileWatcherManager = new FileWatcherManager(path);
                _currentFolderPath = path;
                _currentFilePath = null;
            }
            else
            {
                _fileWatcherManager.ChangePath(path);
            }
            return _fileWatcherManager;
        }
        public static void StopDirectoryWatcher()
        {
            if (_fileWatcherManager != null)
            {
                _fileWatcherManager.StopFileWatch();//ta funkcja zatrzymuje śledzenie folderów
                _fileWatcherManager = null;
            }
        }

        public static FileWatcherManager GetFileWatcherManager(string filePath, string fileName)
        {
            if (_fileWatcherManager == null || !filePath.Equals(_currentFolderPath, System.StringComparison.OrdinalIgnoreCase))
            {
                StopFileWatcher(); //funkcja zatrzymująca śledzenie folderów

                _fileWatcherManager = new FileWatcherManager(filePath);
                _currentFolderPath = filePath;
            }

                string fullPathToFile = filePath + "\\" + fileName;
                _currentFilePath = fullPathToFile;
                _fileWatcherManager.WatchFileContentChanges(fullPathToFile);
            return _fileWatcherManager;
        }

        public static void StopFileWatcher()
        {
            if(_fileWatcherManager != null)
            {
                _fileWatcherManager.StopFileContentWatch();
                _fileWatcherManager = null;
            }
        }

        public static string GetCurrentWatcherItemPath()
        {
            if(_fileWatcherManager != null)
            {
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    return "F$" + _currentFilePath;//dodajemy prefix F$, aby później rozróżnić, że to plik
                }
                else if(!string.IsNullOrEmpty(_currentFolderPath))
                {
                    return "D$" + _currentFolderPath;//dodajemy prefiź D$, aby później rozróżnić, że do folder
                }
            }
            return null;
        }
    }
}
