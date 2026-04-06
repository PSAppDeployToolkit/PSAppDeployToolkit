using System.Collections.Generic;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a handle for reading standard input or output from a process, including the associated asynchronous
    /// task and the buffered output lines.
    /// </summary>
    internal sealed record ProcessReadStream : ProcessStream
    {
        /// <summary>
        /// Initializes a new instance of the ProcessReadStream class with the specified buffer and task.
        /// </summary>
        /// <param name="buffer">A read-only list containing the lines of text that have been read from the process stream.</param>
        /// <param name="task">The task that performs the asynchronous read operation from the process stream. Cannot be null.</param>
        internal ProcessReadStream(IReadOnlyList<string> buffer, Task task) : base(task)
        {
            Buffer = buffer;
        }

        /// <summary>
        /// Gets a read-only list containing the lines of text that have been read from the process stream.
        /// </summary>
        internal IReadOnlyList<string> Buffer { get; }
    }
}
