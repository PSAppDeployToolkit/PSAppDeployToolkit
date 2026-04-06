using System;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents an abstract base class for managing a standard input, output, or error handle associated with a
    /// process, providing resource management and disposal functionality.
    /// </summary>
    internal abstract record ProcessStream : IDisposable
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
        /// Releases all resources used by the current instance.
        /// </summary>
        /// <remarks>Call this method when you are finished using the object to free unmanaged resources
        /// immediately. After calling this method, the object should not be used.</remarks>
        public void Dispose()
        {
            if (Task.IsCompleted)
            {
                Task.Dispose();
            }
        }

        /// <summary>
        /// Represents the underlying asynchronous operation associated with this instance.
        /// </summary>
        internal readonly Task Task;
    }
}
