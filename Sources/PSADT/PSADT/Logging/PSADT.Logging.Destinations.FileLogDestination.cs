using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PSADT.Logging.Models;
using PSADT.Logging.Utilities;
using PSADT.Logging.Interfaces;

namespace PSADT.Logging.Destinations
{
    /// <summary>
    /// Handles logging to a file with log rotation and archiving capabilities.
    /// </summary>
    public class FileLogDestination : ILogDestination, IDisposable
    {
        private readonly string _logFilePath;
        private readonly ulong _maxLogFileSizeInBytes;
        private readonly TextLogFormat _logFormat;
        private readonly string _textSeparator;

        private readonly object _fileLock = new object();
        private StreamWriter _streamWriter;

        private readonly SemaphoreSlim _fileWriteSemaphore = new SemaphoreSlim(1, 1);
        private bool _disposed = false;

        public FileLogDestination(LogOptions logOptions)
        {
            _logFilePath = logOptions.LogFilePath;
            _maxLogFileSizeInBytes = logOptions.MaxLogFileSizeInBytes;
            _logFormat = logOptions.LogFormat;
            _textSeparator = logOptions.TextSeparator;

            InitializeWriter();

            if (_streamWriter == null)
            {
                throw new InvalidOperationException("Failed to initialize the log file writer.");
            }
        }

        /// <summary>
        /// Initializes the StreamWriter.
        /// </summary>
        private void InitializeWriter()
        {
            lock (_fileLock)
            {
                var logDirectory = Path.GetDirectoryName(_logFilePath) ?? throw new InvalidOperationException("Invalid log directory.");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                _streamWriter = new StreamWriter(new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new System.Text.UTF8Encoding(false))
                {
                    AutoFlush = true
                };
            }
        }

        /// <summary>
        /// Writes a log entry to the file asynchronously.
        /// </summary>
        /// <param name="logEntry">The log entry to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task WriteLogEntryAsync(LogEntry logEntry)
        {
            try
            {
                if (_disposed) throw new ObjectDisposedException(nameof(FileLogDestination));

                await _fileWriteSemaphore.WaitAsync();
                try
                {
                    var formattedMessage = logEntry.FormatMessage(_logFormat, _textSeparator);
                    await _streamWriter.WriteLineAsync(formattedMessage);
                    await CheckAndRotateAsync();
                }
                finally
                {
                    _fileWriteSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                await Task.Run(() => SharedLoggerUtilities.LogToEventLogAsync($@"Failed to write log entry to file:{Environment.NewLine}{logEntry.Message}", ex));
            }
        }

        /// <summary>
        /// Checks the log file size and rotates the log if necessary.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CheckAndRotateAsync()
        {
            try
            {
                var fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Exists && fileInfo.Length >= (long)_maxLogFileSizeInBytes)
                {
                    _streamWriter?.Dispose();

                    string archiveFileName = $"{Path.GetFileNameWithoutExtension(_logFilePath)}_{DateTime.UtcNow:yyyyMMddHHmmss}.log";
                    string archiveFilePath = Path.Combine(Path.GetDirectoryName(_logFilePath)!, archiveFileName);
                    File.Move(_logFilePath, archiveFilePath);

                    // Optionally, implement archiving logic (e.g., compressing old logs)

                    // Re-initialize the writer for the new log file
                    InitializeWriter();
                }
            }
            catch (Exception ex)
            {
                await SharedLoggerUtilities.LogToEventLogAsync($@"Failed to rotate the log file: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            lock (_fileLock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _streamWriter?.Flush();
                    _streamWriter?.Dispose();
                    _fileWriteSemaphore?.Dispose();
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}
