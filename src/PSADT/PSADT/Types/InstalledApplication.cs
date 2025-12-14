using PSADT.ProcessManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PSADT.Types
{
    /// <summary>
    /// Represents an installed application and its related information.
    /// </summary>
    public abstract record InstalledApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstalledApplication"/> record.
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
        protected InstalledApplication(
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
            bool is64BitApplication)
        {
            PSPath = !string.IsNullOrWhiteSpace(psPath) ? psPath : throw new ArgumentNullException("PSPath cannot be null or empty.", (Exception?)null);
            PSParentPath = !string.IsNullOrWhiteSpace(psParentPath) ? psParentPath : throw new ArgumentNullException("PSParentPath cannot be null or empty.", (Exception?)null);
            PSChildName = !string.IsNullOrWhiteSpace(psChildName) ? psChildName : throw new ArgumentNullException("PSChildName cannot be null or empty.", (Exception?)null);
            DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : throw new ArgumentNullException("DisplayName cannot be null or empty.", (Exception?)null);
            DisplayVersion = !string.IsNullOrWhiteSpace(displayVersion) ? displayVersion : null;
            UninstallString = !string.IsNullOrWhiteSpace(uninstallString) ? uninstallString : null;
            InstallSource = installSource;
            InstallLocation = installLocation;
            InstallDate = installDate;
            Publisher = !string.IsNullOrWhiteSpace(publisher) ? publisher : null;
            EstimatedSize = estimatedSize;
            Is64BitApplication = is64BitApplication;
            if (UninstallString is not null)
            {
                var argumentList = CommandLineUtilities.CommandLineToArgumentList(UninstallString);
                UninstallStringFilePath = new(argumentList[0]);
                if (argumentList.Count > 1)
                {
                    UninstallStringArgumentList = new ReadOnlyCollection<string>(argumentList.Skip(1).ToArray());
                }
            }

        }

        /// <summary>
        /// Returns a string representation of the installed application.
        /// </summary>
        /// <returns>A string that contains key details about the installed application.</returns>
        public override string ToString() => $"Installed Application: {DisplayName} (Version: {DisplayVersion}, Publisher: {Publisher})";

        /// <summary>
        /// Gets the registry key that contains the uninstall entry.
        /// </summary>
        public string PSPath { get; }

        /// <summary>
        /// Gets the registry key for the subkey's parent.
        /// </summary>
        public string PSParentPath { get; }

        /// <summary>
        /// Gets the registry subkey for uninstalling the application.
        /// </summary>
        public string PSChildName { get; }

        /// <summary>
        /// Gets the display name of the application.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the version of the application.
        /// </summary>
        public string? DisplayVersion { get; }

        /// <summary>
        /// Gets the uninstall string used to remove the application.
        /// </summary>
        public string? UninstallString { get; }

        /// <summary>
        /// Gets the file path to the uninstall string, if available.
        /// </summary>
        public FileInfo? UninstallStringFilePath { get; }

        /// <summary>
        /// Gets the uninstall arguments used to remove the application as a list.
        /// </summary>
        public IReadOnlyList<string>? UninstallStringArgumentList { get; }

        /// <summary>
        /// Gets the source from which the application was installed.
        /// </summary>
        public DirectoryInfo? InstallSource { get; }

        /// <summary>
        /// Gets the location where the application is installed.
        /// </summary>
        public DirectoryInfo? InstallLocation { get; }

        /// <summary>
        /// Gets the date the application was installed as a <see cref="DateTime"/> object.
        /// </summary>
        public DateTime? InstallDate { get; }

        /// <summary>
        /// Gets the publisher of the application.
        /// </summary>
        public string? Publisher { get; }

        /// <summary>
        /// Gets the estimated disk usage on kilobytes of the application.
        /// </summary>
        public uint? EstimatedSize { get; }

        /// <summary>
        /// Gets a value indicating whether the application is a 64-bit application.
        /// </summary>
        public bool Is64BitApplication { get; }
    }
}
