using System;
using System.Globalization;

namespace PSADT.ShortcutManagement
{
    /// <summary>
    /// Represents the base class for shortcuts, containing common properties.
    /// </summary>
    public abstract record ShortcutBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShortcutBase"/> class with optional properties.
        /// </summary>
        /// <param name="path">The path to the shortcut file.</param>
        /// <param name="targetPath">The target path of the shortcut.</param>
        /// <param name="iconIndex">The index of the icon used for the shortcut.</param>
        /// <param name="iconLocation">The location of the icon used for the shortcut.</param>
        private protected ShortcutBase(string path, string targetPath, string? iconLocation, string? iconIndex)
        {
            if (iconLocation is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(iconLocation);
            }
            if (iconIndex is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(iconIndex);
                IconIndex = int.Parse(iconIndex, CultureInfo.InvariantCulture);
            }
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
            Path = path;
            TargetPath = targetPath;
            IconLocation = iconLocation;
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
