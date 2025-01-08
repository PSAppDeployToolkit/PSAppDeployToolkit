namespace PSADT.Types
{
    /// <summary>
    /// Represents a URL shortcut.
    /// </summary>
    public class ShortcutUrl : ShortcutBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShortcutBase"/> class with optional properties.
        /// </summary>
        /// <param name="path">The path to the shortcut file.</param>
        /// <param name="targetPath">The target path of the shortcut.</param>
        /// <param name="iconIndex">The index of the icon used for the shortcut.</param>
        /// <param name="iconLocation">The location of the icon used for the shortcut.</param>
        public ShortcutUrl(string? path = null, string? targetPath = null, string? iconIndex = null, string? iconLocation = null) : base(path, targetPath, iconIndex, iconLocation)
        {
        }
    }
}
