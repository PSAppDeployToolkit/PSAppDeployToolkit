namespace PSADT.Diagnostics.Exceptions
{
    /// <summary>
    /// Enum representing the type of system error being handled (Win32 or COM).
    /// </summary>
    public enum SystemErrorType
    {
        /// <summary>
        /// Represents a Win32 error.
        /// </summary>
        Win32,

        /// <summary>
        /// Represents a COM error.
        /// </summary>
        COM
    }
}
