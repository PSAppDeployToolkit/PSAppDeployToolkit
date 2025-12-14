using System;
using System.IO;

namespace PSADT.Types
{
    /// <summary>
    /// Represents an installed MSI application with ARP (Add/Remove Programs) information.
    /// </summary>
    public sealed record InstalledMsiApplication : InstalledArpApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstalledArpApplication"/> record.
        /// </summary>
        public InstalledMsiApplication(
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
            bool systemComponent,
            Guid productCode)
            : base(psPath, psParentPath, psChildName, displayName, displayVersion, uninstallString, installSource, installLocation, installDate, publisher, estimatedSize, is64BitApplication, quietUninstallString, helpLink, true, systemComponent)
        {
            ProductCode = productCode;
        }

        /// <summary>
        /// Gets the product code for the application.
        /// </summary>
        public readonly Guid ProductCode;
    }
}
