using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using PSADT.Interop;
using Windows.Win32.Storage.Packaging.Appx;
using Windows.Win32.System.Com;

namespace PSADT.AppManagement
{
    /// <summary>
    /// Represents information about a Windows Runtime package.
    /// </summary>
    public sealed record class AppxManifestInfo
    {
        /// <summary>
        /// Retrieves the manifest information from the specified package file by determining the package type and reading the appropriate manifest file within the package archive.
        /// </summary>
        /// <param name="packageUri">The URI to the package file.</param>
        /// <returns>The manifest information.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the package file is not a valid Appx/Msix package or bundle.</exception>
        public static AppxManifestInfo Get(Uri packageUri)
        {
            return DeterminePackageType(packageUri) switch
            {
                AppxPackageType.Package => GetPackageManifestInformation(packageUri),
                AppxPackageType.Bundle => GetBundleManifestInformation(packageUri),
                _ => throw new InvalidOperationException("The specified package file is not a valid Appx/Msix package or bundle."),
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppxManifestInfo"/> record based on the provided <see cref="IAppxManifestPackageId"/>.
        /// </summary>
        /// <param name="packageId">The package ID from which to initialize the package information.</param>
        /// <param name="packageType">The type of the package (single package or bundle).</param>
        private AppxManifestInfo(IAppxManifestPackageId packageId, AppxPackageType packageType)
        {
            Name = packageId.GetName().ToString();
            Architecture = (Interop.APPX_PACKAGE_ARCHITECTURE)packageId.GetArchitecture();
            Publisher = packageId.GetPublisher().ToString();
            ulong versionValue = packageId.GetVersion();
            Version = new((int)(versionValue >> 48), (int)((versionValue >> 32) & 0xFFFF), (int)((versionValue >> 16) & 0xFFFF), (int)(versionValue & 0xFFFF));
            ResourceId = packageId.GetResourceId().ToString();
            PackageFullName = packageId.GetPackageFullName().ToString();
            PackageFamilyName = packageId.GetPackageFamilyName().ToString();
            PackageType = packageType;
        }

        /// <summary>
        /// Gets the name of the package as defined in the manifest.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the processor architecture as defined in the manifest.
        /// </summary>
        public Interop.APPX_PACKAGE_ARCHITECTURE Architecture { get; }

        /// <summary>
        /// Gets the name of the package publisher as defined in the manifest.
        /// </summary>
        public string Publisher { get; }

        /// <summary>
        /// Gets the version of the package as defined in the manifest.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Gets the package resource identifier as defined in the manifest.
        /// </summary>
        public string ResourceId { get; }

        /// <summary>
        /// Gets the package full name.
        /// </summary>
        public string PackageFullName { get; }

        /// <summary>
        /// Gets the package family name.
        /// </summary>
        public string PackageFamilyName { get; }

        /// <summary>
        /// Gets the type of the package (single package or bundle) based on the presence of specific manifest files within the package archive.
        /// </summary>
        public AppxPackageType PackageType { get; }

        /// <summary>
        /// Determines the type of the package (single package or bundle) by checking for the presence of specific manifest files within the package archive.
        /// </summary>
        /// <param name="packageUri">The URI to the package file.</param>
        /// <returns>The type of the package.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the package file is not a valid Appx/Msix package or bundle.</exception>
        private static AppxPackageType DeterminePackageType(Uri packageUri)
        {
            // Check for the presence of a bundle manifest file to determine if it's a bundle.
            using ZipArchive archive = ZipFile.OpenRead(packageUri.LocalPath);
            return archive.GetEntry("AppxMetadata/AppxBundleManifest.xml") is not null ? AppxPackageType.Bundle : archive.GetEntry("AppxManifest.xml") is not null ? AppxPackageType.Package
                : throw new InvalidOperationException("The specified package file is not a valid Appx/Msix package or bundle.");
        }

        /// <summary>
        /// Reads the package information from the specified package file by opening it as an IStream and using the AppxFactory COM interfaces.
        /// </summary>
        /// <param name="packageUri">The URI to the package file.</param>
        /// <returns>The package information.</returns>
        private static AppxManifestInfo GetPackageManifestInformation(Uri packageUri)
        {
            IStream packageStream = CreateStreamForPackage(packageUri);
            try
            {
                IAppxFactory appxFactory = (IAppxFactory)new AppxFactory();
                try
                {
                    IAppxPackageReader packageReader = appxFactory.CreatePackageReader(packageStream);
                    try
                    {
                        IAppxManifestReader manifestReader = packageReader.GetManifest();
                        try
                        {
                            IAppxManifestPackageId packageId = manifestReader.GetPackageId();
                            try
                            {
                                return new(packageId, AppxPackageType.Package);
                            }
                            finally
                            {
                                _ = Marshal.FinalReleaseComObject(packageId);
                            }
                        }
                        finally
                        {
                            _ = Marshal.FinalReleaseComObject(manifestReader);
                        }
                    }
                    finally
                    {
                        _ = Marshal.FinalReleaseComObject(packageReader);
                    }
                }
                finally
                {
                    _ = Marshal.FinalReleaseComObject(appxFactory);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(packageStream);
            }
        }

        /// <summary>
        /// Reads the package information from the specified bundle file by opening it as an IStream and using the AppxBundleFactory COM interfaces.
        /// </summary>
        /// <param name="packageUri">The URI to the bundle file.</param>
        /// <returns>The package information.</returns>
        private static AppxManifestInfo GetBundleManifestInformation(Uri packageUri)
        {
            IStream packageStream = CreateStreamForPackage(packageUri);
            try
            {
                IAppxBundleFactory appxFactory = (IAppxBundleFactory)new AppxBundleFactory();
                try
                {
                    IAppxBundleReader packageReader = appxFactory.CreateBundleReader(packageStream);
                    try
                    {
                        IAppxBundleManifestReader manifestReader = packageReader.GetManifest();
                        try
                        {
                            IAppxManifestPackageId packageId = manifestReader.GetPackageId();
                            try
                            {
                                return new(packageId, AppxPackageType.Bundle);
                            }
                            finally
                            {
                                _ = Marshal.FinalReleaseComObject(packageId);
                            }
                        }
                        finally
                        {
                            _ = Marshal.FinalReleaseComObject(manifestReader);
                        }
                    }
                    finally
                    {
                        _ = Marshal.FinalReleaseComObject(packageReader);
                    }
                }
                finally
                {
                    _ = Marshal.FinalReleaseComObject(appxFactory);
                }
            }
            finally
            {
                _ = Marshal.FinalReleaseComObject(packageStream);
            }
        }

        /// <summary>
        /// Creates an IStream for the specified package file using the SHCreateStreamOnFileEx function from the Windows API.
        /// </summary>
        /// <param name="packageUri">The URI to the package file.</param>
        /// <returns>An IStream representing the package file.</returns>
        private static IStream CreateStreamForPackage(Uri packageUri)
        {
            _ = NativeMethods.SHCreateStreamOnFileEx(packageUri.LocalPath, Interop.STGM.STGM_READ | Interop.STGM.STGM_SHARE_DENY_NONE, FileAttributes.Normal, fCreate: false, pstmTemplate: null, out IStream ppstm);
            return ppstm;
        }
    }
}
