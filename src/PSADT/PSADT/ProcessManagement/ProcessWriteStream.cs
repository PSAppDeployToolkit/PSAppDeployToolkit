using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a write handle for standard input or output associated with a process, enabling asynchronous write
    /// operations.
    /// </summary>
    /// <param name="Task">The task that manages the asynchronous operation for the standard I/O write handle. Must not be null.</param>
    internal sealed record ProcessWriteStream(Task Task) : ProcessStream(Task);
}
