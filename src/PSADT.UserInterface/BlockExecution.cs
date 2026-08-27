namespace PSADT.UserInterface
{
    /// <summary>
    /// Provides constants related to blocking execution, including registry key names and dialog button text.
    /// </summary>
    /// <remarks>This class contains only static members and cannot be instantiated. Use these constants when
    /// interacting with dialogs or registry entries that pertain to blocking execution functionality.</remarks>
    public static class BlockExecution
    {
        /// <summary>
        /// Specifies the registry key name used to store the block execution command.
        /// </summary>
        public const string RegistryKeyName = "BlockExecutionCommand";

        /// <summary>
        /// Gets the text for the button used to block execution in a dialog.
        /// </summary>
        public const string ButtonText = "OK";
    }
}
