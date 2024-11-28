namespace PSADT.ProcessEx
{
    /// <summary>
    /// Specifies options for waiting on processes.
    /// </summary>
    public enum WaitType
    {
        /// <summary>
        /// Wait for any process to exit.
        /// </summary>
        WaitForAny,

        /// <summary>
        /// Wait for all processes to exit.
        /// </summary>
        WaitForAll
    }
}