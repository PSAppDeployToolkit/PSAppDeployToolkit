using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents an abstract base class for managing a standard input, output, or error handle associated with a
    /// process, providing resource management and disposal functionality.
    /// </summary>
    /// <param name="Task">The task to associate with the process stream. Cannot be null.</param>
    internal abstract record class ProcessStream(Task Task)
    {
        /// <summary>
        /// Represents the underlying asynchronous operation associated with this instance.
        /// </summary>
        internal readonly Task Task = Task;
    }
}
