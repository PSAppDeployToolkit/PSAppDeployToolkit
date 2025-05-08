using System;

namespace PSADT.Types
{
    /// <summary>
    /// Represents the base class for shortcuts, containing common properties.
    /// </summary>
    public abstract record ShortcutBase
    {
        /// <summary>
        /// The path to the shortcut file.
        /// </summary>
        public readonly string? Path;

        /// <summary>
        /// The target path of the shortcut.
        /// </summary>
        public readonly string? TargetPath;

        /// <summary>
        /// The index of the icon used for the shortcut.
        /// </summary>
        public readonly string? IconIndex;

        /// <summary>
        /// The location of the icon used for the shortcut.
        /// </summary>
        public readonly string? IconLocation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShortcutBase"/> class with optional properties.
        /// </summary>
        /// <param name="path">The path to the shortcut file.</param>
        /// <param name="targetPath">The target path of the shortcut.</param>
        /// <param name="iconIndex">The index of the icon used for the shortcut.</param>
        /// <param name="iconLocation">The location of the icon used for the shortcut.</param>
        protected ShortcutBase(string? path = null, string? targetPath = null, string? iconIndex = null, string? iconLocation = null)
        {
            if (!string.IsNullOrWhiteSpace(path) && !IsValidFilePath(path!))
            {
                throw new ArgumentException("Invalid file path provided for the shortcut.");
            }
            Path = path;

            if (!string.IsNullOrWhiteSpace(targetPath) && !IsValidTargetPath(targetPath!))
            {
                throw new ArgumentException("Invalid target path provided for the shortcut.");
            }
            TargetPath = targetPath;

            if (!string.IsNullOrWhiteSpace(iconIndex) && !int.TryParse(iconIndex, out _))
            {
                throw new ArgumentException("IconIndex must be a numeric value.");
            }
            IconIndex = iconIndex;

            if (!string.IsNullOrWhiteSpace(iconLocation) && !IsValidFilePath(iconLocation!))
            {
                throw new ArgumentException("Invalid icon location path provided.");
            }
            IconLocation = iconLocation;
        }

        /// <summary>
        /// Validates whether the provided string is a valid file path.
        /// </summary>
        /// <param name="path">The file path to validate.</param>
        /// <returns>True if the file path is valid, otherwise false.</returns>
        private static bool IsValidFilePath(string path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) && System.IO.Path.IsPathRooted(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates whether the provided string is a valid target path, which could be a file or a URL.
        /// </summary>
        /// <param name="targetPath">The target path to validate.</param>
        /// <returns>True if the target path is valid, otherwise false.</returns>
        private static bool IsValidTargetPath(string targetPath)
        {
            return Uri.TryCreate(targetPath, UriKind.Absolute, out _);
        }
    }
}
