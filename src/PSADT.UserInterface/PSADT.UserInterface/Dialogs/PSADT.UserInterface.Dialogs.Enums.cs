namespace PSADT.UserInterface.Dialogs
{
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
        TopCenter,
    }

    /// <summary>
    /// Defines the alignment of the message text in the dialog.
    /// </summary>
    public enum DialogMessageAlignment
    {
        /// <summary>
        /// Aligns the message text to the left
        /// </summary>
        Left,

        /// <summary>
        /// Aligns the message text to the center
        /// </summary>
        Center,

        /// <summary>
        /// Aligns the message text to the right
        /// </summary>
        Right,
    }

    /// <summary>
    /// Defines the system icons that can be used in the dialog.
    /// </summary>
    public enum DialogSystemIcon
    {
        /// <summary>
        /// Icon for generic application
        /// </summary>
        Application,

        /// <summary>
        /// Icon for asterisk
        /// </summary>
        Asterisk,

        /// <summary>
        /// Icon for error
        /// </summary>
        Error,

        /// <summary>
        /// Icon for exclamation
        /// </summary>
        Exclamation,

        /// <summary>
        /// Icon for hand
        /// </summary>
        Hand,

        /// <summary>
        /// Icon for information
        /// </summary>
        Information,

        /// <summary>
        /// Icon for question
        /// </summary>
        Question,

        /// <summary>
        /// Icon for shield
        /// </summary>
        Shield,

        /// <summary>
        /// Icon for stop
        /// </summary>
        Warning,

        /// <summary>
        /// Icon for the Windows logo
        /// </summary>
        WinLogo,
    }
}
