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
        /// Initializes a new instance of the <see cref="InstalledMsiApplication"/> record.
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
        /// <param name="productCode">The product code for the application.</param>
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
            : base(psPath, psParentPath, psChildName, displayName, displayVersion, uninstallString, installSource, installLocation, installDate, publisher, estimatedSize, is64BitApplication, quietUninstallString, helpLink, systemComponent)
        {
            ProductCode = productCode;
        }

        /// <summary>
        /// Gets the product code for the application.
        /// </summary>
        public Guid ProductCode { get; }

        /// <summary>
        /// Gets a value indicating whether the application is an MSI.
        /// </summary>
        public override bool WindowsInstaller => true;
    }
}
