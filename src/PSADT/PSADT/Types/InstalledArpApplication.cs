using PSADT.ProcessManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PSADT.Types
{
    /// <summary>
    /// Represents an installed application with ARP (Add/Remove Programs) information.
    /// </summary>
    public record InstalledArpApplication : InstalledApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstalledArpApplication"/> record.
        /// </summary>
        public InstalledArpApplication(
            string psPath,
            string psParentPath,
            string psChildName,
            string displayName,
            string? displayVersion,
            string? uninstallString,
            DirectoryInfo? installSource,
            DirectoryInfo? installLocation,
            DateTime? installDate,
            string? publisher,
            uint? estimatedSize,
            bool is64BitApplication,
            string? quietUninstallString,
            Uri? helpLink,
            bool windowsInstaller,
            bool systemComponent)
            : base(psPath, psParentPath, psChildName, displayName, displayVersion, uninstallString, installSource, installLocation, installDate, publisher, estimatedSize, is64BitApplication)
        {
            HelpLink = helpLink;
            WindowsInstaller = windowsInstaller;
            SystemComponent = systemComponent;
            QuietUninstallString = !string.IsNullOrWhiteSpace(quietUninstallString) ? quietUninstallString : null;
            if (QuietUninstallString is not null)
            {
                var argumentList = CommandLineUtilities.CommandLineToArgumentList(QuietUninstallString);
                QuietUninstallStringFilePath = new(argumentList[0]);
                if (argumentList.Count > 1)
                {
                    QuietUninstallStringArgumentList = new ReadOnlyCollection<string>(argumentList.Skip(1).ToArray());
                }
            }
        }

        /// <summary>
        /// Gets the quiet uninstall string used to remove the application.
        /// </summary>
        public readonly string? QuietUninstallString;

        /// <summary>
        /// Gets the file path to the quiet uninstall string, if available.
        /// </summary>
        public readonly FileInfo? QuietUninstallStringFilePath;

        /// <summary>
        /// Gets the quiet uninstall arguments used to remove the application as a list.
        /// </summary>
        public readonly IReadOnlyList<string>? QuietUninstallStringArgumentList;

        /// <summary>
        /// Gets the publisher's help link of the application.
        /// </summary>
        public readonly Uri? HelpLink;

        /// <summary>
        /// Gets a value indicating whether the application is a system component.
        /// </summary>
        public readonly bool SystemComponent;

        /// <summary>
        /// Gets a value indicating whether the application is an MSI.
        /// </summary>
        public readonly bool WindowsInstaller;
    }
}
