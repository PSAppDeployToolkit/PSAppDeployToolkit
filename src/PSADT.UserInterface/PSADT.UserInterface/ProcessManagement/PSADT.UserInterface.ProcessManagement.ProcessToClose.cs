namespace PSADT.UserInterface.ProcessManagement
{
    /// <summary>
    /// Represents a process that needs to be closed.
    /// </summary>
    public sealed class ProcessToClose
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessToClose"/> class.
        /// </summary>
        /// <param name="process"></param>
        internal ProcessToClose(RunningProcess runningProcess)
        {
            Name = runningProcess.Process.ProcessName;
            Path = runningProcess.FileName;
            Description = runningProcess.Description;
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets the path of the process.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        public readonly string Description;
    }
}
