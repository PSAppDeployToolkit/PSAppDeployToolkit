using System;
using System.Collections.ObjectModel;

namespace PSADT.PackageManagement
{
    /// <summary>
    /// Represents the data stored in a Appx manifest.
    /// </summary>
    public sealed record AppxManifest
    {
        internal AppxManifest(
            string name,
            string publisher,
            string version,
            ProcessorArchitecture architecture,
            string resourceId,
            string publisherId,
            string fullIdentifier,
            string familyIdentifier,
            string path,
            bool isBundle,
            ReadOnlyCollection<string>? bundledApplications = null,
            ReadOnlyCollection<string>? bundledResources = null)
        {
            Name = name;
            Publisher = publisher;
            Version = version;
            Architecture = architecture;
            ResourceId = resourceId;
            PublisherId = publisherId;
            FullNameIdentifier = fullIdentifier;
            FamilyIdentifer = familyIdentifier;
            Path = path;
            IsBundle = isBundle;
            BundledApplications = bundledApplications ?? new ReadOnlyCollection<string>(Array.Empty<string>());
            BundledResources = bundledResources ?? new ReadOnlyCollection<string>(Array.Empty<string>()); ;
        }

        /// <summary>
        /// The name of the application.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The fully qualified name of the publisher.
        /// </summary>
        public string Publisher {  get; }
        /// <summary>
        /// The version of the application.
        /// </summary>
        public string Version { get; }
        /// <summary>
        /// The archtiecture of the application.
        /// </summary>
        public ProcessorArchitecture Architecture {  get; }
        /// <summary>
        /// The resource id of the application, if applicable.
        /// </summary>
        public string ResourceId {  get; }
        /// <summary>
        /// The encoded publisher name.
        /// </summary>
        public string PublisherId {  get; }
        /// <summary>
        /// The fully qualified id of the application.
        /// </summary>
        public string FullNameIdentifier { get; }
        /// <summary>
        /// The family identifier of the application.
        /// </summary>
        public string FamilyIdentifer {  get; }
        /// <summary>
        /// The path the manifest was read from.
        /// </summary>
        public string Path {  get; }
        /// <summary>
        /// Determines wether the manifest parsed was a bundle.
        /// </summary>
        public bool IsBundle { get; }
        /// <summary>
        /// If this manifest represents a bundle, the application ids it packages.
        /// </summary>
        public ReadOnlyCollection<string> BundledApplications { get; }
        /// <summary>
        /// If this manifest represents a bundle, the bundled resource ids it packages.
        /// </summary>
        public ReadOnlyCollection<string> BundledResources { get; }
    }
}
