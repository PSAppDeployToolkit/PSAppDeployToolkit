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
            bool isFramework)
            : base(psPath, psParentPath, psChildName, displayName, displayVersion, uninstallString, installSource, installLocation, installDate, publisher, estimatedSize, is64BitApplication)
        {
            FullName = !string.IsNullOrWhiteSpace(fullName) ? fullName : throw new ArgumentNullException("FullName cannot be null or empty.", (Exception?)null);
            FamilyName = !string.IsNullOrWhiteSpace(familyName) ? familyName : throw new ArgumentNullException("FamilyName cannot be null or empty.", (Exception?)null);
            PublisherId = !string.IsNullOrWhiteSpace(publisherId) ? publisherId : throw new ArgumentNullException("PublisherId cannot be null or empty.", (Exception?)null);
            Architecture = !string.IsNullOrWhiteSpace(architecture) ? architecture : throw new ArgumentNullException("Architecture cannot be null or empty.", (Exception?)null);
            IsBundle = isBundle;
            IsResource = isResource;
            IsFramework = isFramework;
        }

        /// <summary>
        /// Gets the package full name of the package.
        /// </summary>
        public readonly string FullName;

        /// <summary>
        /// Gets the family name this package belongs to.
        /// </summary>
        public readonly string FamilyName;

        /// <summary>
        /// Gets the publisher ID of the package.
        /// </summary>
        public readonly string PublisherId;

        /// <summary>
        /// Gets the name of the architecture of the package.
        /// </summary>
        public readonly string Architecture;

        /// <summary>
        /// Indicates whether the package is a bundle.
        /// </summary>
        public readonly bool IsBundle;

        /// <summary>
        /// Indicates whether the package is a resource package.
        /// </summary>
        public readonly bool IsResource;

        /// <summary>
        /// Indicates whether the package is a framework package.
        /// </summary>
        public readonly bool IsFramework;
    }
}
