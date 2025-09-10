using System;

namespace PSADT.Types
{
    /// <summary>
    /// Represents the base class for shortcuts, containing common properties.
    /// </summary>
    public abstract class ShortcutBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShortcutBase"/> class with optional properties.
        /// </summary>
        /// <param name="path">The path to the shortcut file.</param>
        /// <param name="targetPath">The target path of the shortcut.</param>
        /// <param name="iconIndex">The index of the icon used for the shortcut.</param>
        /// <param name="iconLocation">The location of the icon used for the shortcut.</param>
        protected ShortcutBase(string path, string targetPath, string? iconLocation, string? iconIndex)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid file path provided for the shortcut.");
            }
            Path = path;

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                throw new ArgumentException("Invalid target path provided for the shortcut.");
            }
            TargetPath = targetPath;
            IconLocation = iconLocation;

            if (!string.IsNullOrWhiteSpace(iconIndex) && !int.TryParse(iconIndex, out var IconIndex))
            {
                throw new ArgumentException("IconIndex must be a numeric value.");
            }
        }

        /// <summary>
        /// The path to the shortcut file.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// The target path of the shortcut.
        /// </summary>
        public readonly string TargetPath;

        /// <summary>
        /// The location of the icon used for the shortcut.
        /// </summary>
        public readonly string? IconLocation;

        /// <summary>
        /// The index of the icon used for the shortcut.
        /// </summary>
        public readonly int? IconIndex;
    }
}
