namespace PSADT.UserInterface
{
    /// <summary>
    /// Defines the position of the dialog window on the screen
    /// </summary>
    public enum DialogPosition
    {
        /// <summary>
        /// Represents the default location for the dialog.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Represents the top-left corner of the screen.
        /// </summary>
        TopLeft = 1,

        /// <summary>
        /// Represents the top-middle area of the screen.
        /// </summary>
        Top = 2,

        /// <summary>
        /// Represents the top-right corner of the screen.
        /// </summary>
        TopRight = 3,

        /// <summary>
        /// Represents the top-middle area of the screen, half way between the top and center.
        /// </summary>
        TopCenter = 4,

        /// <summary>
        /// Represents the center of the screen.
        /// </summary>
        Center = 5,

        /// <summary>
        /// Represents the bottom-left corner of the screen.
        /// </summary>
        BottomLeft = 6,

        /// <summary>
        /// Represents the bottom-middle area of the screen.
        /// </summary>
        Bottom = 7,

        /// <summary>
        /// Represents the bottom-right corner of the screen.
        /// </summary>
        BottomRight = 8,

        /// <summary>
        /// Represents the bottom-middle area of the screen, half way between the bottom and center.
        /// </summary>
        BottomCenter = 9,

        /// <summary>
        /// Represents a center-left position designed for OOBE (Out Of Box Experience) screens.
        /// The dialog is vertically centered and horizontally offset to the left from the center of the screen.
        /// </summary>
        Oobe = 10,
    }
}
