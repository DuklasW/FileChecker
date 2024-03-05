using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffMatchPatch;

namespace FileWatcherLibrary
{
    public class FileWatcherManager
    {
        public readonly FileSystemWatcher _fileSystemWatcher;
        public event EventHandler<string> FileChangeEvent; //foldery
        public event EventHandler<string> FileContentChangedEvent;//pliki
        public event EventHandler<string> FileDeletedEvent;
        public event EventHandler<string> FileChangeActiveEvent;


        public string CurrentFilePath { get; private set; }
        private string[] _previousLines;


        //funkcja tworzy watcher na folder
        public FileWatcherManager(string path)
        {
            CurrentFilePath = null;
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The directory '{path}' does not exist.");
            }

            _fileSystemWatcher = new FileSystemWatcher(path)
            {
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.Attributes | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };

            _fileSystemWatcher.Changed += OnActionOccurOnFolderPath;
            _fileSystemWatcher.Created += OnActionOccurOnFolderPath;
            _fileSystemWatcher.Deleted += OnFileDeleted;
            _fileSystemWatcher.Renamed += OnFileRenameOccur;
        }

        //obsługa śledzenia plików w folderze
        private void OnActionOccurOnFolderPath(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                FileChangeEvent?.Invoke(this, $"---Some directory change occur---\n{e.ChangeType} -> {e.Name}");
            }
            else if (File.Exists(e.FullPath))
            {
                FileChangeEvent?.Invoke(this, $"---Some file change occur---\n{e.ChangeType} -> {e.Name}");
            }
                
        }

        //obsługa śledzenia plików w folderze
        private void OnFileRenameOccur(object sender, RenamedEventArgs e)
        {
            if (string.Equals(e.FullPath, CurrentFilePath, StringComparison.OrdinalIgnoreCase))
            {
                FileChangeActiveEvent?.Invoke(this, e.FullPath);
            }
            else
            {
                if (Directory.Exists(e.FullPath))
                {
                    FileChangeEvent?.Invoke(this, $"---Directory changed---\n{e.OldName} => {e.Name}");
                }
                else if (File.Exists(e.FullPath))
                {
                    FileChangeEvent?.Invoke(this, $"---File name changed---\n{e.OldName} => {e.Name}");
                }
            }
        }


        //zatrzymanie śledzenia folderu
        public void StopFileWatch()
        {
            _fileSystemWatcher.Changed -= OnActionOccurOnFolderPath;
            _fileSystemWatcher.Created -= OnActionOccurOnFolderPath;
            _fileSystemWatcher.Deleted -= OnFileDeleted;
            _fileSystemWatcher.Renamed -= OnFileRenameOccur;

            _fileSystemWatcher.EnableRaisingEvents = false;
        }


        //obsługa śledzenia pliku
        public void WatchFileContentChanges(string filePath)
        {
            CurrentFilePath = filePath;

            _fileSystemWatcher.Changed -= OnActionOccurOnFolderPath;
            _fileSystemWatcher.Created -= OnActionOccurOnFolderPath;
            
            _previousLines = File.ReadAllLines(CurrentFilePath);

            _fileSystemWatcher.Renamed += OnFileRenameOccur;
            _fileSystemWatcher.Changed += OnFileContentChanged;            
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                if (string.Equals(e.FullPath, CurrentFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    CurrentFilePath = e.FullPath;
                    FileDeletedEvent?.Invoke(this, e.FullPath);
                }
                else
                {
                    if(!e.FullPath.Equals("select.this.directory.this.directory"))
                    {
                        FileChangeEvent?.Invoke(this, $"---Deleted---\n{e.Name}");
                    }
                   
                }
            }
            else
            {
                if (!e.FullPath.Equals("select.this.directory.this.directory"))
                {
                    FileChangeEvent?.Invoke(this, $"---Deleted---\n{e.Name}");
                }
            }
        }


        //obsługa zmian w środku pliku
        private void OnFileContentChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                if (string.Equals(e.FullPath, CurrentFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    string[] newLines = File.ReadAllLines(CurrentFilePath);

                    var diffMatchPatch = new diff_match_patch();
                    List<Diff> diffs = diffMatchPatch.diff_main(string.Join("\n", _previousLines), string.Join("\n", newLines));

                    StringBuilder output = new StringBuilder();

                    foreach (var diff in diffs)
                    {
                        if (!string.IsNullOrEmpty(diff.text) && !string.IsNullOrWhiteSpace(diff.text.Trim('\n')))
                        {
                            var lines = diff.text.Split('\n');

                            foreach (var line in lines)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    if (diff.operation == Operation.INSERT)
                                    {
                                        output.Append("+\t" + line.Trim('\n') + "\n");
                                    }
                                    else if (diff.operation == Operation.DELETE)
                                    {
                                        output.Append("-\t" + line.Trim('\n') + "\n");
                                    }
                                }
                                
                            }
                        }
                    }

                    string combinedDiff = output.ToString();
                    FileContentChangedEvent?.Invoke(this, combinedDiff);

                    _previousLines = newLines;
                }
            }
        }


        //koniec śledzenia pliku(zmian w nim)
        public void StopFileContentWatch()
        {
            _fileSystemWatcher.Deleted -= OnFileDeleted;
            _fileSystemWatcher.Changed -= OnFileContentChanged;
            _fileSystemWatcher.Renamed -= OnFileRenameOccur;
            CurrentFilePath = null;
        }


        //zmienia ścieżkę śledzonego folderu - potrzebne do działania singleton service
        public void ChangePath(string newPath)
        {
            StopFileWatch();
            _fileSystemWatcher.Path = newPath;
            StartFileWatch();
        }

        //start file
        public void StartFileWatch()
        {
            StopFileWatch();

            _fileSystemWatcher.Changed += OnActionOccurOnFolderPath;
            _fileSystemWatcher.Created += OnActionOccurOnFolderPath;
            _fileSystemWatcher.Deleted += OnFileDeleted;
            _fileSystemWatcher.Renamed += OnFileRenameOccur;

            _fileSystemWatcher.EnableRaisingEvents = true;
        }
    }
}