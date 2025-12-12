namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides common constants and utilities for client-server communication.
    /// </summary>
    /// <remarks>This class contains shared definitions used in inter-process communication, such as the
    /// character used to separate command parameters in pipe-based communication.</remarks>
    public static class CommonUtilities
    {
        /// <summary>
        /// Represents the character used to separate command parameters in pipe communication.
        /// </summary>
        /// <remarks>The separator is defined as the Unicode character with the value 0x1F. This character
        /// is used internally to delimit parameters in inter-process communication.</remarks>
        public const char ArgumentSeparator = (char)0x1F;
    }
}
