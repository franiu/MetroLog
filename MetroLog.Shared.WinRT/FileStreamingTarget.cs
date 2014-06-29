using Windows.Storage.Streams;
using MetroLog.Layouts;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace MetroLog.Targets
{
    /// <summary>
    /// Defines a target that will append messages to a single file.
    /// </summary>
    public class FileStreamingTarget : WinRTFileTarget
    {
        public FileStreamingTarget()
            : this(new SingleLineLayout())
        {
        }

        public FileStreamingTarget(Layout layout)
            : base(layout)
        {
            this.FileNamingParameters.IncludeLevel = false;
            this.FileNamingParameters.IncludeLogger = false;
            this.FileNamingParameters.IncludeSequence = false;
            this.FileNamingParameters.IncludeSession = false;
            this.FileNamingParameters.IncludeTimestamp = FileTimestampMode.Date;
            FileNamingParameters.CreationMode = FileCreationMode.AppendIfExisting;
        }

        protected override Task WriteTextToFileCore(IStorageFile file, string contents)
        {
            // The original code causes serious issue in intensive multi-threaded scenarios throwing
            // "File could not be replaced" exception, even though appending.
            //return FileIO.AppendTextAsync(file, contents + Environment.NewLine).AsTask();

            return Task.Run(async () =>
            {
                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (var outputStream = fileStream.GetOutputStreamAt(fileStream.Size))
                    {
                        using (var dataWriter = new DataWriter(outputStream))
                        {
                            dataWriter.WriteString(contents + Environment.NewLine);
                            await dataWriter.StoreAsync().AsTask();
                            dataWriter.DetachStream();
                        }

                        await outputStream.FlushAsync();
                    }
                }
            });
        }
    }
}
