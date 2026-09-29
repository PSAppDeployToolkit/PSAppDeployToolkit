using System.IO.Pipes;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a write handle for standard input or output associated with a process, enabling asynchronous write
    /// operations.
    /// </summary>
    /// <param name="Stream">The anonymous pipe server stream used for writing to the process. Cannot be null.</param>
    /// <param name="Task">The task that performs the asynchronous write operation to the process stream. Cannot be null.</param>
    internal sealed class ProcessWriteStream(AnonymousPipeServerStream Stream, Task Task) : ProcessStream(Stream, Task);
}
