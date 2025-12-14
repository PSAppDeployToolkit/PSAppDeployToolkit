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
        /// <param name="psPath">The registry key that contains the uninstall entry.</param>
        /// <param name="psParentPath">The registry key for the subkey's parent.</param>
        /// <param name="psChildName">The registry subkey for uninstalling the application.</param>
        /// <param name="displayName">The display name of the application.</param>
        /// <param name="displayVersion">The version of the application.</param>
        /// <param name="uninstallString">The uninstall string used to remove the application.</param>
        /// <param name="installSource">The source from which the application was installed.</param>
        /// <param name="installLocation">The location where the application is installed.</param>
        /// <param name="installDate">The date the application was installed (as a string).</param>
        /// <param name="publisher">The publisher of the application.</param>
        /// <param name="estimatedSize">The estimated on-disk usage of the application.</param>
        /// <param name="is64BitApplication">A value indicating whether the application is a 64-bit application.</param>
        /// <param name="quietUninstallString">The quiet uninstall string used to remove the application.</param>
        /// <param name="helpLink">The publisher's help link of the application.</param>
        /// <param name="systemComponent">A value indicating whether the application is a system component.</param>
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
            bool systemComponent)
            : base(psPath, psParentPath, psChildName, displayName, displayVersion, uninstallString, installSource, installLocation, installDate, publisher, estimatedSize, is64BitApplication)
        {
            HelpLink = helpLink;
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
        public string? QuietUninstallString { get; }

        /// <summary>
        /// Gets the file path to the quiet uninstall string, if available.
        /// </summary>
        public FileInfo? QuietUninstallStringFilePath { get; }

        /// <summary>
        /// Gets the quiet uninstall arguments used to remove the application as a list.
        /// </summary>
        public IReadOnlyList<string>? QuietUninstallStringArgumentList { get; }

        /// <summary>
        /// Gets the publisher's help link of the application.
        /// </summary>
        public Uri? HelpLink { get; }

        /// <summary>
        /// Gets a value indicating whether the application is a system component.
        /// </summary>
        public bool SystemComponent { get; }

        /// <summary>
        /// Gets a value indicating whether the application is an MSI.
        /// </summary>
        public virtual bool WindowsInstaller => false;
    }
}
