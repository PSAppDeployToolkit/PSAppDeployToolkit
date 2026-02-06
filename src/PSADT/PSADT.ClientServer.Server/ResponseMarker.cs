namespace PSADT.ClientServer
{
    /// <summary>
    /// Defines the byte markers used to indicate success or error in pipe communication responses.
    /// </summary>
    /// <remarks>
    /// The response format is: [1-byte marker][serialized data]
    /// <list type="bullet">
    /// <item><description><see cref="Success"/>: Data contains the serialized result of type T.</description></item>
    /// <item><description><see cref="Error"/>: Data contains a serialized <see cref="System.Exception"/>.</description></item>
    /// </list>
    /// This convention follows Win32 BOOL semantics where non-zero indicates success.
    /// </remarks>
    internal enum ResponseMarker : byte
    {
        /// <summary>
        /// Indicates the operation failed. The response data contains a serialized exception.
        /// </summary>
        Error = 0x00,

        /// <summary>
        /// Indicates the operation succeeded. The response data contains the serialized result.
        /// </summary>
        Success = 0x01,
    }
}
