using System;
using System.IO;

namespace PSADT.Types
{
    /// <summary>
    /// Represents an installed Appx application.
    /// </summary>
    public sealed record InstalledAppxPackage : InstalledApplication
    {
        /// <summary>
        /// Creates a new instance of the <see cref="InstalledAppxPackage"/> record.
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
        /// <param name="fullName">The full name of the Appx package.</param>
        /// <param name="familyName">The family name of the Appx package.</param>
        /// <param name="publisherId">The publisher ID of the Appx package.</param>
        /// <param name="architecture">The architecture of the Appx package.</param>
        /// <param name="isBundle">A value indicating whether the Appx package is a bundle.</param>
        /// <param name="isResource">A value indicating whether the Appx package is a resource.</param>
        /// <param name="isFramework">A value indicating whether the Appx package is a framework.</param>
        /// <param name="nonRemovable">A value indicating whether the Appx package is non-removable.</param>
        /// <param name="provisionedPackage">A value indicating whether the Appx package is provisioned.</param>
        public InstalledAppxPackage(
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
            string fullName,
            string familyName,
            string publisherId,
            string architecture,
            bool isBundle,
            bool isResource,
            bool isFramework,
            bool nonRemovable,
            string? provisionedPackage)
            : base(psPath, psParentPath, psChildName, displayName, displayVersion, uninstallString, installSource, installLocation, installDate, publisher, estimatedSize, is64BitApplication)
        {
            FullName = !string.IsNullOrWhiteSpace(fullName) ? fullName : throw new ArgumentNullException("FullName cannot be null or empty.", (Exception?)null);
            FamilyName = !string.IsNullOrWhiteSpace(familyName) ? familyName : throw new ArgumentNullException("FamilyName cannot be null or empty.", (Exception?)null);
            PublisherId = !string.IsNullOrWhiteSpace(publisherId) ? publisherId : throw new ArgumentNullException("PublisherId cannot be null or empty.", (Exception?)null);
            Architecture = !string.IsNullOrWhiteSpace(architecture) ? architecture : throw new ArgumentNullException("Architecture cannot be null or empty.", (Exception?)null);
            IsBundle = isBundle;
            IsResource = isResource;
            IsFramework = isFramework;
            NonRemovable = nonRemovable;

            if (!string.IsNullOrWhiteSpace(provisionedPackage))
            {
                IsProvisioned = true;
                ProvisionedPackage = provisionedPackage;
            }
        }

        /// <summary>
        /// Gets the package full name of the package.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the family name this package belongs to.
        /// </summary>
        public string FamilyName { get; }

        /// <summary>
        /// Gets the publisher ID of the package.
        /// </summary>
        public string PublisherId { get; }

        /// <summary>
        /// Gets the name of the architecture of the package.
        /// </summary>
        public string Architecture { get; }

        /// <summary>
        /// Indicates whether the package is a bundle.
        /// </summary>
        public bool IsBundle { get; }

        /// <summary>
        /// Indicates whether the package is a resource package.
        /// </summary>
        public bool IsResource { get; }

        /// <summary>
        /// Indicates whether the package is a framework package.
        /// </summary>
        public bool IsFramework { get; }

        /// <summary>
        /// Indicates whether the package can be removed.
        /// </summary>
        public bool NonRemovable { get; }

        /// <summary>
        /// Indicates whether the package is provisioned.
        /// </summary>
        public bool IsProvisioned { get; }

        /// <summary>
        /// Gets the full name of the package that provisioned this package, if applicable.
        /// </summary>
        public string? ProvisionedPackage { get; }
    }
}
