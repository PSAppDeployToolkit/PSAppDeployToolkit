using System;
using System.Text.RegularExpressions;

namespace PSADT.Types
{
    /// <summary>
    /// Represents an LNK (Windows Shortcut) file.
    /// </summary>
    public sealed class ShortcutLnk : ShortcutBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShortcutLnk"/> class with specified properties.
        /// </summary>
        /// <param name="arguments">Arguments passed to the target application.</param>
        /// <param name="description">Description of the shortcut.</param>
        /// <param name="workingDirectory">Working directory for the target application.</param>
        /// <param name="windowStyle">Window style (e.g., normal, minimized, maximized).</param>
        /// <param name="hotkey">Hotkey associated with the shortcut.</param>
        /// <param name="runAsAdmin">Indicates if the shortcut requires administrative privileges.</param>
        public ShortcutLnk(string path, string targetPath, string? iconLocation, string? iconIndex, string? arguments, string? description, string? workingDirectory, string? windowStyle, string? hotkey, bool runAsAdmin) : base(path, targetPath, iconLocation, iconIndex)
        {
            if (string.IsNullOrWhiteSpace(windowStyle) || !Regex.IsMatch(windowStyle, "^(Normal|Minimized|Maximized)$", RegexOptions.IgnoreCase))
            {
                throw new ArgumentException($"Invalid window style: {windowStyle}. Must be one of: Normal, Minimized, Maximized.");
            }
            WindowStyle = windowStyle;
            Arguments = arguments;
            Description = description;
            WorkingDirectory = workingDirectory;
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
            {
                return false;
            }
            string[] parts = Hotkey!.Split('+');
            if (parts.Length < 2)
            {
                return false;
            }

            // Check if it contains a valid modifier
            string[] validModifiers = { "Ctrl", "Alt", "Shift" };
            string[] validKeys = { "A-Z", "0-9", "F1-F12", "Insert", "Delete", "Home", "End" };
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
        /// The window style for the shortcut.
        /// </summary>
        public readonly string? WindowStyle;

        /// <summary>
        /// Gets or sets the arguments passed to the target application when the shortcut is executed.
        /// </summary>
        public readonly string? Arguments;

        /// <summary>
        /// Gets or sets the description of the shortcut.
        /// </summary>
        public readonly string? Description;

        /// <summary>
        /// Gets or sets the working directory for the shortcut's target application.
        /// </summary>
        public readonly string? WorkingDirectory;

        /// <summary>
        /// Gets or sets the hotkey associated with the shortcut.
        /// </summary>
        public readonly string? Hotkey;

        /// <summary>
        /// Gets or sets a value indicating whether the shortcut requires administrative privileges to run.
        /// </summary>
        public readonly bool RunAsAdmin;

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
}
