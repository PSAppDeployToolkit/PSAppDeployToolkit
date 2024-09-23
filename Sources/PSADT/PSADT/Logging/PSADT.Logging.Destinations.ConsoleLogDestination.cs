using System;
using System.Threading.Tasks;
using PSADT.Logging.Interfaces;
using PSADT.Logging.Models;

namespace PSADT.Logging.Destinations
{
    public class ConsoleLogDestination : ILogDestination
    {
        private static readonly object _consoleLock = new object();

        public async Task WriteLogEntryAsync(LogEntry logEntry)
        {
            var formattedMessage = logEntry.FormatMessage(TextLogFormat.Standard, " | ");

            // Ensure single-threaded writes to avoid interleaved console output
            await Task.Run(() =>
            {
                lock (_consoleLock)
                {
                    Console.WriteLine(formattedMessage);
                }
            });
        }
    }
}
