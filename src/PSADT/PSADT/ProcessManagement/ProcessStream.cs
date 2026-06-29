using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents an abstract base class for managing a standard input, output, or error handle associated with a
    /// process, providing resource management and disposal functionality.
    /// </summary>
    internal abstract record class ProcessStream
    {
        /// <summary>
        /// Initializes a new instance of the ProcessStream class with the specified task.
        /// </summary>
        /// <param name="task">The task to associate with the process stream. Cannot be null.</param>
        private protected ProcessStream(Task task)
        {
            Task = task;
        }

        /// <summary>
        /// Represents the underlying asynchronous operation associated with this instance.
        /// </summary>
        internal readonly Task Task;
    }
}
