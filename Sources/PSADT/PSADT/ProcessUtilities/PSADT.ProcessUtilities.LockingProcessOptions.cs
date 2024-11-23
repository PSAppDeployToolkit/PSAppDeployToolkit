namespace PSADT.ProcessUtilities
{
    /// <summary>
    /// Options for controlling the behavior of GetLockingProcessesInfo.
    /// </summary>
    public class LockingProcessesOptions
    {
        /// <summary>
        /// If true, will recursively search subdirectories when checking a folder.
        /// </summary>
        public bool Recursive { get; set; } = false;

        /// <summary>
        /// Maximum depth to search when Recursive is true. Set to -1 for unlimited. Default is 2.
        /// </summary>
        public int MaxDepth { get; set; } = 2;

        /// <summary>
        /// If true, will continue on access denied errors. If false, will throw exception.
        /// </summary>
        public bool ContinueOnAccessDenied { get; set; } = true;
    }
}
