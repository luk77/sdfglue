//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppFramework.Platform
{
    // Monitors file changes in a directory (recursively).
    // FileSystemWatcher events arrive on another thread, so instead of an event there is
    // a flag polled once per frame (ConsumeChange) - we want at most one reaction per frame.
    public class FileChangeMonitor : IDisposable
    {
        private FileSystemWatcher?      watcher_                = null;
        private volatile bool           changeDetected_         = false;

        public FileChangeMonitor(string pathToFolder, string[] filters)
        {
            watcher_ = new FileSystemWatcher(pathToFolder);

            watcher_.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 //| NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher_.Changed += OnChanged;
            watcher_.Created += OnCreatedDeletedRenamed;
            watcher_.Deleted += OnCreatedDeletedRenamed;
            watcher_.Renamed += OnCreatedDeletedRenamed;
            watcher_.Error   += OnError;

            foreach (var filter in filters)
                watcher_.Filters.Add(filter);

            watcher_.IncludeSubdirectories = true;
            watcher_.EnableRaisingEvents = true;
        }

        // Returns true if a change was detected since the last call (and clears the flag)
        public bool ConsumeChange()
        {
            if (!changeDetected_)
                return false;

            changeDetected_ = false;
            return true;
        }

        public void Dispose()
        {
            if (watcher_ != null)
            {
                watcher_.Dispose();
                watcher_ = null;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            changeDetected_ = true;
        }

        private void OnCreatedDeletedRenamed(object sender, FileSystemEventArgs e)
        {
            changeDetected_ = true;
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            PrintException(e.GetException());
        }

        private static void PrintException(Exception? ex)
        {
            if (ex != null)
            {
                Console.WriteLine($"FSW: Message: {ex.Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                PrintException(ex.InnerException);
            }
        }
    }
}
