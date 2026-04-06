using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a write handle for standard input or output associated with a process, enabling asynchronous write
    /// operations.
    /// </summary>
    internal sealed record ProcessWriteStream : ProcessStream
    {
        /// <summary>
        /// Initializes a new instance of the ProcessWriteStream class with the specified task.
        /// </summary>
        /// <param name="task">The task that manages the asynchronous operation for the standard I/O write handle. Must not be null.</param>
        internal ProcessWriteStream(Task task) : base(task)
        {
        }
    }
}
