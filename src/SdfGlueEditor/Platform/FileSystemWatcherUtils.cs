//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Collections.ObjectModel;

namespace SdfGlueEditor.Platform
{
    internal static class FileSystemWatcherUtils
    {
        public delegate void ChangeDetectedDelegate();

        private static FileSystemWatcher? watcher_ = null;

        //public static event ChangeDetectedDelegate? OnChangeDetected;
        // We want only one event per frame
        public static bool ChangeDetected = false;

        //public static void Initialize(string pathToFolder, string filter)
        //{
        //    Initialize(pathToFolder, new string[] { filter } );
        //}

        public static void Initialize(string pathToFolder, string[] filters)
        {
            if (watcher_ != null)
            {
                Console.WriteLine("WARNING: FileSystemWatcher already initialized.");
                return;
            }

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
            watcher_.Created += OnCreated;
            watcher_.Deleted += OnDeleted;
            watcher_.Renamed += OnRenamed;
            watcher_.Error += OnError;

            //watcher_.Filter = filter;
            foreach (var filter in filters)
                watcher_.Filters.Add(filter);

            watcher_.IncludeSubdirectories = true;
            watcher_.EnableRaisingEvents = true;

        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            //Console.WriteLine($"FSW: Changed: {e.FullPath}");
            //if (OnChangeDetected != null)
            //    OnChangeDetected();
            ChangeDetected = true;
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            //string value = $"FSW: Created: {e.FullPath}";
            //Console.WriteLine(value);
            //if (OnChangeDetected != null)
            //    OnChangeDetected();
            ChangeDetected = true;
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            //Console.WriteLine($"FSW: Deleted: {e.FullPath}");
            //if (OnChangeDetected != null)
            //    OnChangeDetected();
            ChangeDetected = true;
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            //Console.WriteLine($"FSW: Renamed:");
            //Console.WriteLine($"    Old: {e.OldFullPath}");
            //Console.WriteLine($"    New: {e.FullPath}");
            //if (OnChangeDetected != null)
            //    OnChangeDetected();
            ChangeDetected = true;
        }

        private static void OnError(object sender, ErrorEventArgs e)
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
