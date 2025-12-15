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

            if (!string.IsNullOrWhiteSpace(iconIndex))
            {
                if (!int.TryParse(iconIndex, out var parsedIndex))
                {
                    throw new ArgumentException("IconIndex must be a numeric value.");
                }
                IconIndex = parsedIndex;
            }
        }

        /// <summary>
        /// The path to the shortcut file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The target path of the shortcut.
        /// </summary>
        public string TargetPath { get; }

        /// <summary>
        /// The location of the icon used for the shortcut.
        /// </summary>
        public string? IconLocation { get; }

        /// <summary>
        /// The index of the icon used for the shortcut.
        /// </summary>
        public int? IconIndex { get; }
    }
}
