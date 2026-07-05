using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a write handle for standard input or output associated with a process, enabling asynchronous write
    /// operations.
    /// </summary>
    /// <param name="Task">The task that performs the asynchronous write operation to the process stream. Cannot be null.</param>
    internal sealed class ProcessWriteStream(Task Task) : ProcessStream(Task);
}
