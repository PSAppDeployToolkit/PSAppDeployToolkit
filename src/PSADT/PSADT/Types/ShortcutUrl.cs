namespace PSADT.Types
{
    /// <summary>
    /// Represents a URL shortcut.
    /// </summary>
    public sealed record ShortcutUrl : ShortcutBase
    {
        /// <summary>
        /// Initializes a new instance of the ShortcutUrl class with the specified shortcut file path, target path, icon
        /// location, and icon index.
        /// </summary>
        /// <param name="path">The file system path where the shortcut will be created. Cannot be null or empty.</param>
        /// <param name="targetPath">The path to the target file or URL that the shortcut points to. Cannot be null or empty.</param>
        /// <param name="iconLocation">The file system path to the icon to be used for the shortcut, or null to use the default icon.</param>
        /// <param name="iconIndex">The index of the icon within the icon file specified by iconLocation, or null to use the default index.</param>
        public ShortcutUrl(string path, string targetPath, string? iconLocation, string? iconIndex) : base(path, targetPath, iconLocation, iconIndex)
        {
        }
    }
}
