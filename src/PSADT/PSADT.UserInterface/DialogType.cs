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
        CloseAppsDialog = 0,

        /// <summary>
        /// Represents the CustomDialog type.
        /// </summary>
        CustomDialog = 1,

        /// <summary>
        /// Represents a Windows 9x-style message box.
        /// </summary>
        DialogBox = 2,

        /// <summary>
        /// Provides methods for displaying help information in the console.
        /// </summary>
        HelpConsole = 3,

        /// <summary>
        /// Represents the InputDialog type.
        /// </summary>
        InputDialog = 4,

        /// <summary>
        /// Represents the ListSelectionDialog type.
        /// </summary>
        ListSelectionDialog = 5,

        /// <summary>
        /// Represents the ProgressDialog type.
        /// </summary>
        ProgressDialog = 6,

        /// <summary>
        /// Represents the RestartDialog type.
        /// </summary>
        RestartDialog = 7,
    }
}
