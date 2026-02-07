namespace PSADT.UserInterface
{
    /// <summary>
    /// Specifies the types of dialogs that can be displayed in the application.
    /// </summary>
    /// <remarks>The <see cref="DialogType"/> enumeration defines various dialog types that can be used to
    /// interact with users. Each value represents a specific type of dialog, such as message boxes, input dialogs, or
    /// progress dialogs. This enumeration is typically used to identify or configure the type of dialog to
    /// display.</remarks>
    public enum DialogType
    {
        /// <summary>
        /// Represents the CloseAppsDialog type.
        /// </summary>
        CloseAppsDialog,

        /// <summary>
        /// Represents the CustomDialog type.
        /// </summary>
        CustomDialog,

        /// <summary>
        /// Represents a Windows 9x-style message box.
        /// </summary>
        DialogBox,

        /// <summary>
        /// Provides methods for displaying help information in the console.
        /// </summary>
        HelpConsole,

        /// <summary>
        /// Represents the InputDialog type.
        /// </summary>
        InputDialog,

        /// <summary>
        /// Represents the ProgressDialog type.
        /// </summary>
        ProgressDialog,

        /// <summary>
        /// Represents the RestartDialog type.
        /// </summary>
        RestartDialog,
    }
}
