﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MetroLog.Layouts;
using MetroLog.Targets;
using Windows.Storage;
using Windows.Storage.Search;

namespace MetroLog
{
    public abstract class WinRTFileTarget : FileTargetBase
    {
        private static StorageFolder _logFolder = null;
        private readonly Semaphore _writeSemaphore;


        protected WinRTFileTarget(Layout layout) : base(layout)
        {
            _writeSemaphore = new Semaphore(1,1);
        }

        public static async Task<StorageFolder> EnsureInitializedAsync()
        {
            if (_logFolder == null)
            {
                var root = ApplicationData.Current.LocalFolder;

                _logFolder = await root.CreateFolderAsync(LogFolderName, CreationCollisionOption.OpenIfExists);
                
            }
            return _logFolder;
        }

        protected override async Task<Stream> GetCompressedLogsInternal()
        {
            var ms = new MemoryStream();

            await ZipFile.CreateFromDirectory(_logFolder, ms);
            ms.Position = 0;

            return ms;
        }
        
        protected override Task EnsureInitialized()
        {
            return EnsureInitializedAsync();
        }

        sealed protected override async Task DoCleanup(Regex pattern, DateTime threshold)
        {

            var toDelete = new List<StorageFile>();
            foreach (var file in await _logFolder.GetFilesAsync())
            {
                if (pattern.Match(file.Name).Success && file.DateCreated <= threshold)
                    toDelete.Add(file);
            }

            
            var qo = new QueryOptions(CommonFileQuery.DefaultQuery, new [] {".zip"})
                {
                    FolderDepth = FolderDepth.Shallow,
                    UserSearchFilter = "System.FileName:~<\"Log -\""
                };

            var query = ApplicationData.Current.TemporaryFolder.CreateFileQueryWithOptions(qo);

            var oldLogs = await query.GetFilesAsync();
            toDelete.AddRange(oldLogs);

            // walk...
            foreach (var file in toDelete)
            {
                try
                {
                    await file.DeleteAsync();
                }
                catch (Exception ex)
                {
                    InternalLogger.Current.Warn(string.Format("Failed to delete '{0}'.", file.Path), ex);
                }
            }
        }

        protected sealed override async Task<LogWriteOperation> DoWriteAsync(string fileName, string contents, LogEventInfo entry)
        {
            // write...

            var file = await _logFolder.CreateFileAsync(fileName, FileNamingParameters.CreationMode == FileCreationMode.AppendIfExisting ? CreationCollisionOption.OpenIfExists : CreationCollisionOption.ReplaceExisting);

            _writeSemaphore.WaitOne();
            try
            {
                WriteTextToFileCore(file, contents).Wait();
            }
            finally
            {
                _writeSemaphore.Release();
            }

            // return...
            return new LogWriteOperation(this, entry, true);
        }

        protected abstract Task WriteTextToFileCore(IStorageFile file, string contents);
    }
}
