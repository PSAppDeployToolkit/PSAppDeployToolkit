using System.Threading.Tasks;
using PSADT.Logging.Models;

namespace PSADT.Logging.Interfaces
{
    public interface ILogDestination
    {
        Task WriteLogEntryAsync(LogEntry logEntry);
    }
}
