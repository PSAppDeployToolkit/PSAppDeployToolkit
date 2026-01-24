namespace PSADT.ClientServer
{
    /// <summary>
    /// Represents the exit codes that can be returned by the application to indicate the result of its execution.
    /// </summary>
    public enum ClientExitCode : int
    {
        /// <summary>
        /// The client operation completed successfully.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The client operation failed due to an unknown error.
        /// </summary>
        Unknown = 1,

        /// <summary>
        /// The client operation failed because no arguments were provided.
        /// </summary>
        NoArguments = 2,

        /// <summary>
        /// The client operation failed due to invalid arguments.
        /// </summary>
        InvalidArguments = 3,

        /// <summary>
        /// The client operation failed due to an invalid mode being specified.
        /// </summary>
        InvalidMode = 4,

        /// <summary>
        /// The client operation failed because no options were provided.
        /// </summary>
        NoOptions = 5,

        /// <summary>
        /// The client operation failed due to invalid options.
        /// </summary>
        InvalidOptions = 6,

        /// <summary>
        /// The client operation failed due to an invalid result being returned.
        /// </summary>
        InvalidResult = 7,

        /// <summary>
        /// The client operation failed because no dialog was specified.
        /// </summary>
        NoDialogType = 10,

        /// <summary>
        /// The client operation failed due to an invalid dialog type being specified.
        /// </summary>
        InvalidDialog = 11,

        /// <summary>
        /// The client operation failed because the specified dialog type is not supported.
        /// </summary>
        UnsupportedDialog = 12,

        /// <summary>
        /// The client operation failed due to an invalid dialog style being specified.
        /// </summary>
        NoDialogStyle = 13,

        /// <summary>
        /// The client operation failed due to an invalid dialog style being specified.
        /// </summary>
        InvalidDialogStyle = 14,

        /// <summary>
        /// The client operation failed due to an invalid close applications dialog state being specified.
        /// </summary>
        NoCloseAppsDialogState = 15,

        /// <summary>
        /// The CloseMainWindow() call to close a process failed.
        /// </summary>
        PromptToSaveFailure = 16,

        /// <summary>
        /// The window to which keys were to be sent is not enabled.
        /// </summary>
        SendKeysWindowNotEnabled = 17,

        /// <summary>
        /// The client operation failed because no output pipe was specified.
        /// </summary>
        NoOutputPipe = 20,

        /// <summary>
        /// The client operation failed because no input pipe was specified.
        /// </summary>
        NoInputPipe = 21,

        /// <summary>
        /// The client operation failed because no log pipe was specified.
        /// </summary>
        NoLogPipe = 22,

        /// <summary>
        /// The client operation failed due to an invalid output pipe being specified.
        /// </summary>
        InvalidOutputPipe = 23,

        /// <summary>
        /// The client operation failed due to an invalid input pipe being specified.
        /// </summary>
        InvalidInputPipe = 24,

        /// <summary>
        /// The client operation failed due to an invalid log pipe being specified.
        /// </summary>
        InvalidLogPipe = 25,

        /// <summary>
        /// The client operation failed due to an error reading from or writing to a pipe.
        /// </summary>
        PipeReadWriteError = 26,

        /// <summary>
        /// The client operation failed due to an invalid command being specified.
        /// </summary>
        InvalidCommand = 27,

        /// <summary>
        /// The client operation failed due to an invalid request being made.
        /// </summary>
        InvalidRequest = 28,

        /// <summary>
        /// The client operation failed due to an encryption key exchange or cryptographic error.
        /// </summary>
        EncryptionError = 29,
    }
}
