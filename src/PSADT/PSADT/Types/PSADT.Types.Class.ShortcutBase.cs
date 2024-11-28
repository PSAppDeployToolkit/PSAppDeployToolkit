using System;

namespace PSADT.Types
{
    /// <summary>
    /// Represents the base class for shortcuts, containing common properties.
    /// </summary>
    public abstract class ShortcutBase
    {
        private string? _path;
        private string? _targetPath;
        private string? _iconIndex;
        private string? _iconLocation;

        /// <summary>
        /// Gets or sets the path to the shortcut file. It must be a valid file path.
        /// </summary>
        public string? Path
        {
            get => _path;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !IsValidFilePath(value!))
                {
                    throw new ArgumentException("Invalid file path provided for the shortcut.");
                }
                _path = value;
            }
        }

        /// <summary>
        /// Gets or sets the target path of the shortcut. It must be a valid target file or URL.
        /// </summary>
        public string? TargetPath
        {
            get => _targetPath;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !IsValidTargetPath(value!))
                {
                    throw new ArgumentException("Invalid target path provided for the shortcut.");
                }
                _targetPath = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the icon used for the shortcut. Must be a numeric value or null.
        /// </summary>
        public string? IconIndex
        {
            get => _iconIndex;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !int.TryParse(value, out _))
                {
                    throw new ArgumentException("IconIndex must be a numeric value.");
                }
                _iconIndex = value;
            }
        }

        /// <summary>
        /// Gets or sets the location of the icon used for the shortcut. Must be a valid file path.
        /// </summary>
        public string? IconLocation
        {
            get => _iconLocation;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !IsValidFilePath(value!))
                {
                    throw new ArgumentException("Invalid icon location path provided.");
                }
                _iconLocation = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShortcutBase"/> class with optional properties.
        /// </summary>
        /// <param name="path">The path to the shortcut file.</param>
        /// <param name="targetPath">The target path of the shortcut.</param>
        /// <param name="iconIndex">The index of the icon used for the shortcut.</param>
        /// <param name="iconLocation">The location of the icon used for the shortcut.</param>
        protected ShortcutBase(string? path = null, string? targetPath = null, string? iconIndex = null, string? iconLocation = null)
        {
            Path = path;
            TargetPath = targetPath;
            IconIndex = iconIndex;
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
