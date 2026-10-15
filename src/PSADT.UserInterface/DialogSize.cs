namespace PSADT.UserInterface
{
    /// <summary>
    /// Defines the rendered size of the Fluent dialogs.
    /// </summary>
    /// <remarks>Every value other than <see cref="Auto"/> sizes the dialog as a fraction of the screen's width, so
    /// the dialog covers the same proportion of the display on every endpoint no matter its resolution or the zoom
    /// level its user has configured. The resulting scale is bounded at both ends so that the dialog stays legible on
    /// a small screen and stays proportionate on an ultrawide one.</remarks>
    public enum DialogSize
    {
        /// <summary>
        /// The dialog is drawn at its designed size and therefore follows the display scaling configured in Windows.
        /// This is the default.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// The dialog covers roughly 22% of the screen's width.
        /// </summary>
        Small = 1,

        /// <summary>
        /// The dialog covers roughly 30% of the screen's width.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// The dialog covers roughly 40% of the screen's width.
        /// </summary>
        Large = 3,

        /// <summary>
        /// The dialog covers roughly 50% of the screen's width.
        /// </summary>
        ExtraLarge = 4,
    }
}
