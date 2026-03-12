/*
 * Copyright (C) 2026 Devicie Pty Ltd. All rights reserved.
 * 
 * This file is part of PSAppDeployToolkit. 
 * 
 * PSAppDeployToolkit is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, either version 3
 * of the License, or (at your option) any later version.
 * 
 * PSAppDeployToolkit is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * 
 * See the GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with PSAppDeployToolkit. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

namespace PSADT.ShortcutManagement
{
    /// <summary>
    /// Represents an immutable snapshot of an <see cref="InternetShortcutFile"/>.
    /// </summary>
    public sealed record InternetShortcutInfo : IShortcutLinkInfo
    {
        /// <summary>
        /// Retrieves information about an Internet shortcut file at the specified path.
        /// </summary>
        /// <param name="filePath">The full path to the Internet shortcut file to be analyzed. Cannot be null or empty.</param>
        /// <returns>An InternetShortcutInfo object containing details extracted from the specified shortcut file.</returns>
        public static InternetShortcutInfo Get(string filePath)
        {
            using InternetShortcutFile shortcutFile = InternetShortcutFile.Load(filePath);
            return new(shortcutFile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InternetShortcutInfo"/> class from an <see cref="InternetShortcutFile"/>.
        /// </summary>
        /// <param name="internetShortcut">The internet shortcut to snapshot.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="internetShortcut"/> is null.</exception>
        internal InternetShortcutInfo(InternetShortcutFile internetShortcut)
        {
            ArgumentNullException.ThrowIfNull(internetShortcut);
            FilePath = internetShortcut.FilePath ?? throw new ArgumentNullException(nameof(internetShortcut), "The provided Internet Shortcut does not have a valid file path.");
            Url = internetShortcut.Url;
            Name = internetShortcut.Name;
            WorkingDirectory = internetShortcut.WorkingDirectory;
            Hotkey = internetShortcut.Hotkey;
            ShowCommand = internetShortcut.ShowCommand;
            IconFile = internetShortcut.IconFile;
            IconIndex = internetShortcut.IconIndex;
            WhatsNew = internetShortcut.WhatsNew;
            Author = internetShortcut.Author;
            Description = internetShortcut.Description;
            Comment = internetShortcut.Comment;
            Roamed = internetShortcut.Roamed;
        }

        /// <summary>
        /// Gets the path of the currently loaded shortcut file.
        /// </summary>
        public FileInfo FilePath { get; }

        /// <summary>
        /// Gets the URL of the internet shortcut.
        /// </summary>
        public Uri? Url { get; }

        /// <summary>
        /// Gets the display name for the internet shortcut.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the working directory for the internet shortcut.
        /// </summary>
        public string? WorkingDirectory { get; }

        /// <summary>
        /// Gets the hotkey for the internet shortcut.
        /// </summary>
        public string? Hotkey { get; }

        /// <summary>
        /// Gets the show command value for the internet shortcut.
        /// </summary>
        public ShortcutWindowStyle? ShowCommand { get; }

        /// <summary>
        /// Gets the icon file path for the internet shortcut.
        /// </summary>
        public Uri? IconFile { get; }

        /// <summary>
        /// Gets the icon index for the internet shortcut.
        /// </summary>
        public int? IconIndex { get; }

        /// <summary>
        /// Gets the What's New text for the internet shortcut.
        /// </summary>
        public string? WhatsNew { get; }

        /// <summary>
        /// Gets the author for the internet shortcut.
        /// </summary>
        public string? Author { get; }

        /// <summary>
        /// Gets the description for the internet shortcut.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the comment for the internet shortcut.
        /// </summary>
        public string? Comment { get; }

        /// <summary>
        /// Gets a value indicating whether the internet shortcut has roamed.
        /// </summary>
        public bool? Roamed { get; }
    }
}
