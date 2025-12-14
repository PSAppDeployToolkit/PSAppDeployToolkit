using System.Collections.Generic;

namespace PSADT.Types
{
    /// <summary>
    /// Represents the data stored in a Appx manifest.
    /// </summary>
    /// <remarks>
    /// See https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-properties
    /// </remarks>
    public sealed record PackageManifestInfo
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PackageManifestInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the package.</param>
        /// <param name="version">The version of the package.</param>
        /// <param name="architecture">The architecture of the package.</param>
        /// <param name="resourceId">The resource id of the package, if applicable.</param>
        /// <param name="publisherId">The encoded publisher dn identifier.</param>
        /// <param name="publisherDistinguishedName">The distinguished name of the publisher.</param>
        /// <param name="displayName">The display name of the package.</param>
        /// <param name="publisherDisplayName">The display name of the publisher.</param>
        /// <param name="description">Optional summary of the package.</param>
        /// <param name="isFramework">Indicates that the given package is a framework package.</param>
        /// <param name="isResource">Indicates that the given package is a resource package.</param>
        /// <param name="isBundle">Determines wether the manifest parsed was a bundle.</param>
        /// <param name="bundledApplications">The identifier this package bundles</param>
        /// <param name="bundledResources">The resource identifier this package bundles</param>
        public PackageManifestInfo(
            string name,
            string version,
            string architecture,
            string resourceId,
            string publisherId,
            string publisherDistinguishedName,
            string? displayName,
            string? publisherDisplayName,
            string? description,
            bool isFramework,
            bool isResource,
            bool isBundle,
            IReadOnlyCollection<string> bundledApplications,
            IReadOnlyCollection<string> bundledResources)
        {
            Name = name;
            Version = version;
            Architecture = architecture;
            ResourceId = resourceId;
            PublisherId = publisherId;

            FullName = $"{Name}_{Version}_{Architecture}_{ResourceId}_{PublisherId}";
            FamilyName = $"{Name}_{PublisherId}";

            PublisherDistinguishedName = publisherDistinguishedName;
            DisplayName = displayName;
            PublisherDisplayName = publisherDisplayName;
            Description = description;
            IsFramework = isFramework;
            IsResource = isResource;
            IsBundle = isBundle;
            BundledApplications = bundledApplications;
            BundledResources = bundledResources;
        }

        /// <summary>
        /// The name of the package.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The version of the package.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// The architecture of the package.
        /// </summary>
        public string Architecture { get; }

        /// <summary>
        /// The resource id of the package, if applicable.
        /// </summary>
        public string ResourceId { get; }

        /// <summary>
        /// The encoded publisher dn identifier.
        /// </summary>
        public string PublisherId { get; }

        /// <summary>
        /// The distinguished name of the publisher.
        /// </summary>
        public string PublisherDistinguishedName { get; }

        /// <summary>
        /// The fully qualified id of the package.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// The family identifier of the package.
        /// </summary>
        public string FamilyName { get; }

        /// <summary>
		/// The display name of the package.
		/// </summary>
        public string? DisplayName { get; }

        /// <summary>
        /// The display name of the publisher.
        /// </summary>
        public string? PublisherDisplayName {  get; }

        /// <summary>
        /// Optional summary of the package
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Indicates that the given package is a resource package.
        /// </summary>
        public bool IsResource { get; }

        /// <summary>
        /// Indicates that the given package is a framework package.
        /// </summary>
        public bool IsFramework { get; }

        /// <summary>
        /// Determines wether the manifest parsed was a bundle.
        /// </summary>
        public bool IsBundle { get; }

        /// <summary>
        /// If this manifest represents a bundle, the package identifiers of applications it contains.
        /// </summary>
        public IReadOnlyCollection<string> BundledApplications { get; }

        /// <summary>
        /// If this manifest represents a bundle, the package identifiers of resources it contains.
        /// </summary>
        public IReadOnlyCollection<string> BundledResources { get; }
    }
}
