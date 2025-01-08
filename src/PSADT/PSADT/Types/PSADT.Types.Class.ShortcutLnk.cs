using System;

namespace PSADT.Types
{
    /// <summary>
    /// Represents an LNK (Windows Shortcut) file.
    /// </summary>
    public class ShortcutLnk : ShortcutBase
    {
        private string? _windowStyle;

        /// <summary>
        /// Gets or sets the arguments passed to the target application when the shortcut is executed.
        /// </summary>
        public string? Arguments { get; set; }

        /// <summary>
        /// Gets or sets the description of the shortcut.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the working directory for the shortcut's target application.
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets the window style (e.g., normal, minimized, maximized) for the target application.
        /// This value can be parsed and validated against known window styles.
        /// </summary>
        public string? WindowStyle
        {
            get => _windowStyle;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !Enum.TryParse(value, true, out WindowStyle _))
                {
                    throw new ArgumentException($"Invalid window style: {value}. Must be one of: Normal, Minimized, Maximized.");
                }
                _windowStyle = value;
            }
        }

        /// <summary>
        /// Gets or sets the hotkey associated with the shortcut.
        /// </summary>
        public string? Hotkey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shortcut requires administrative privileges to run.
        /// </summary>
        public bool RunAsAdmin { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShortcutLnk"/> class with specified properties.
        /// </summary>
        /// <param name="arguments">Arguments passed to the target application.</param>
        /// <param name="description">Description of the shortcut.</param>
        /// <param name="workingDirectory">Working directory for the target application.</param>
        /// <param name="windowStyle">Window style (e.g., normal, minimized, maximized).</param>
        /// <param name="hotkey">Hotkey associated with the shortcut.</param>
        /// <param name="runAsAdmin">Indicates if the shortcut requires administrative privileges.</param>
        public ShortcutLnk(string? path, string? targetPath, string? iconIndex, string? iconLocation, string? arguments, string? description, string? workingDirectory, string? windowStyle, string? hotkey, bool runAsAdmin) : base(path, targetPath, iconIndex, iconLocation)
        {
            Arguments = arguments;
            Description = description;
            WorkingDirectory = workingDirectory;
            WindowStyle = windowStyle; // This will trigger validation
            Hotkey = hotkey;
            RunAsAdmin = runAsAdmin;
        }

        /// <summary>
        /// Validates if the provided hotkey follows the required format.
        /// </summary>
        /// <returns>True if the hotkey is valid, otherwise false.</returns>
        public bool IsValidHotkey()
        {
            // Validate the hotkey format based on Windows shortcut criteria.
            if (string.IsNullOrWhiteSpace(Hotkey))
                return false;

            string[] parts = Hotkey!.Split('+');
            if (parts.Length < 2)
                return false;

            string[] validModifiers = { "Ctrl", "Alt", "Shift" };
            string[] validKeys = { "A-Z", "0-9", "F1-F12", "Insert", "Delete", "Home", "End" };

            // Check if it contains a valid modifier
            bool containsModifier = false;
            foreach (var part in parts)
            {
                if (IndexOfAny(part, validModifiers, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    containsModifier = true;
                    break;
                }
            }

            // Check if it contains a valid key
            bool containsValidKey = false;
            foreach (var part in parts)
            {
                if (IndexOfAny(part, validKeys, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    containsValidKey = true;
                    break;
                }
            }

            return containsModifier && containsValidKey;
        }

        /// <summary>
        /// Helper method to check if any string in the list exists within the target string using StringComparison.
        /// </summary>
        /// <param name="target">The target string to search within.</param>
        /// <param name="list">The list of strings to search for.</param>
        /// <param name="comparison">The type of string comparison to use.</param>
        /// <returns>The index of the first occurrence of any string in the list, or -1 if none found.</returns>
        private static int IndexOfAny(string target, string[] list, StringComparison comparison)
        {
            foreach (var item in list)
            {
                int index = target.IndexOf(item, comparison);
                if (index >= 0)
                {
                    return index;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// Specifies the window style for the shortcut.
    /// </summary>
    public enum WindowStyle
    {
        Normal,
        Minimized,
        Maximized
    }
}
