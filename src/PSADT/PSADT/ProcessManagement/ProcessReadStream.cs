using System.Collections.Generic;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a handle for reading standard input or output from a process, including the associated asynchronous
    /// task and the buffered output lines.
    /// </summary>
    /// <param name="Buffer">A read-only list containing the lines of text that have been read from the process stream.</param>
    /// <param name="Task">The task that performs the asynchronous read operation from the process stream. Cannot be null.</param>
    internal sealed record class ProcessReadStream(IReadOnlyList<string> Buffer, Task Task) : ProcessStream(Task)
    {
        /// <summary>
        /// Gets a read-only list containing the lines of text that have been read from the process stream.
        /// </summary>
        internal readonly IReadOnlyList<string> Buffer = Buffer;
    }
}
