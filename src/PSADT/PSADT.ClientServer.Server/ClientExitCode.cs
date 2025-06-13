namespace PSADT.ClientServer
{
    /// <summary>
    /// Represents the exit codes that can be returned by the application to indicate the result of its execution.
    /// </summary>
    public enum ClientExitCode : int
    {
        Success = 0,
        Unknown = 1,
        NoArguments = 2,
        InvalidArguments = 3,
        InvalidMode = 4,
        NoOptions = 5,
        InvalidOptions = 6,
        InvalidResult = 7,

        NoDialogType = 10,
        InvalidDialog = 11,
        UnsupportedDialog = 12,
        NoDialogStyle = 13,
        InvalidDialogStyle = 14,

        NoOutputPipe = 20,
        NoInputPipe = 21,
        NoLogPipe = 22,
        InvalidOutputPipe = 23,
        InvalidInputPipe = 24,
        InvalidLogPipe = 25,
        PipeReadWriteError = 26,
        InvalidCommand = 27,
        InvalidRequest = 28,
    }
}
