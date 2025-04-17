namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Dialog types supported by the UnifiedDialog
    /// </summary>
    public enum DialogType
    {
        /// <summary>
        /// The default dialog type
        /// </summary>
        CloseApps,

        /// <summary>
        /// The dialog type for the progress dialog
        /// </summary>
        Progress,

        /// <summary>
        /// The dialog type for the restart dialog
        /// </summary>
        Restart,

        /// <summary>
        /// The dialog type for the input dialog
        /// </summary>
        Input,

        /// <summary>
        /// The dialog type for the error dialog
        /// </summary>
        Custom
    }

    /// <summary>
    /// Defines the position of the dialog window on the screen
    /// </summary>
    public enum DialogPosition
    {
        /// <summary>
        /// Position in the bottom right corner of the screen (default)
        /// </summary>
        BottomRight,

        /// <summary>
        /// Position in the center of the screen
        /// </summary>
        Center,

        /// <summary>
        /// Position at the top center of the screen
        /// </summary>
        TopCenter
    }
}
