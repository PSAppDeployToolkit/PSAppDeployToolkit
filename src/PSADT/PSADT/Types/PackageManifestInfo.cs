using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSADT.Types
{
    /// <summary>
    /// Contains information parsed from a package manifest.
    /// </summary>
    public sealed record PackageManifestInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageManifestInfo"/> struct.
        /// </summary>
        /// <param name="name">The name of the package.</param>
        /// <param name="version">The version of the package.</param>
        /// <param name="architecture">The architecture of the package.</param>
        /// <param name="resourceId">The resource ID of the package.</param>
        /// <param name="publisherId">The publisher ID of the package.</param>
        /// <param name="publisherDn">The distinguished name of the publisher.</param>
        /// <param name="displayName">The display name of the package.</param>
        /// <param name="publisherDisplayName">The display name of the publisher.</param>
        /// <param name="description">The description of the package.</param>
        /// <param name="isFramework">Indicates whether the package is a framework.</param>
        /// <param name="isResource">Indicates whether the package is a resource.</param>
        /// <param name="isBundle">Indicates whether the package is a bundle.</param>
        /// <param name="bundledApplications">The applications bundled within the package.</param>
        /// <param name="bundledResources">The resources bundled within the package.</param>
        public PackageManifestInfo(
            string name,
            string version,
            string architecture,
            string resourceId,
            string publisherId,
            string publisherDn,
            string displayName,
            string publisherDisplayName,
            string description,
            bool isFramework,
            bool isResource,
            bool isBundle,
            IList<string> bundledApplications,
            IList<string> bundledResources)
        {
            Name = name;
            Version = version;
            Architecture = architecture;
            ResourceId = resourceId;
            PublisherId = publisherId;

            FullName = $"{name}_{version}_{architecture}_{resourceId}_{publisherId}";
            FamilyName = $"{name}_{publisherId}";

            PublisherDn = publisherDn;
            DisplayName = displayName;
            PublisherDisplayName = publisherDisplayName;
            Description = description;
            IsFramework = isFramework;
            IsResource = isResource;
            IsBundle = isBundle;
            BundledApplications = new ReadOnlyCollection<string>(bundledApplications);
            BundledResources = new ReadOnlyCollection<string>(bundledResources);
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
        /// The resource ID of the package.
        /// </summary>
        public string ResourceId { get; }

        /// <summary>
        /// The publisher ID of the package.
        /// </summary>
        public string PublisherId { get; }

        /// <summary>
        /// The full name of the package.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// The family name of the package.
        /// </summary>
        public string FamilyName { get; }

        /// <summary>
        /// The distinguished name of the publisher.
        /// </summary>
        public string PublisherDn { get; }

        /// <summary>
        /// The display name of the package.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The display name of the publisher.
        /// </summary>
        public string PublisherDisplayName { get; }

        /// <summary>
        /// The description of the package.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Indicates whether the package is a framework.
        /// </summary>
        public bool IsFramework { get; }

        /// <summary>
        /// Indicates whether the package is a resource.
        /// </summary>
        public bool IsResource { get; }

        /// <summary>
        /// Indicates whether the package is a bundle.
        /// </summary>
        public bool IsBundle { get; }

        /// <summary>
        /// If the package is a bundle, the applications bundled within the package.
        /// </summary>
        public ReadOnlyCollection<string> BundledApplications { get; }

        /// <summary>
        /// If the package is a bundle, the resources bundled within the package.
        /// </summary>
        public ReadOnlyCollection<string> BundledResources { get; }
    }
}
