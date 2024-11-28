using System;
using System.Threading;
using System.Threading.Tasks;
using PSADT.Logging.Models;
using PSADT.Logging.Interfaces;

namespace PSADT.Logging.Destinations
{
    public class ConsoleLogDestination : ILogDestination
    {
        private static readonly object _consoleLock = new object();

        public Task WriteLogEntryAsync(LogEntry logEntry)
        {
            var formattedMessage = logEntry.FormatMessage(TextLogFormat.Standard, " | ");
            return WriteLineAsync(formattedMessage);
        }

        private static Task WriteLineAsync(string message)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (_consoleLock)
                {
                    Console.WriteLine(message);
                }
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }
    }
}
