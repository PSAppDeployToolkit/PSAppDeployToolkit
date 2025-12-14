using System;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.Packaging.Appx;

namespace PSADT.PackageManagement
{
    /// <summary>
    /// Methods to interact with Appx packages based on identifier
    /// </summary>
    public static class PackageUtilities
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1838:Avoid 'StringBuilder' parameters for P/Invokes", Justification = "This P/Invoke is temporary for now.")]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern WIN32_ERROR PackageFamilyNameFromId(
            PACKAGE_ID packageId,
            ref uint packageFamilyNameLength,
            StringBuilder? packageFamilyName);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
        private struct PACKAGE_ID
        {
            public uint reserved;
            public uint processorArchitecture;
            public PACKAGE_VERSION version;
            public string name;
            public string publisher;
            public string resourceId;
            public string publisherId;
        }

        /// <summary>
        /// Retrieves the package family name from a given package name and publisher.
        /// </summary>
        /// <remarks>
        /// The method will convert the publisherDn into the publisherId to construct the family name.
        /// </remarks>
        /// <param name="name">The identifiying name of the package.</param>
        /// <param name="publisherDn">The distinguished name of the publisher.</param>
        public static string GetPackageFamilyName(string name, string publisherDn)
        {
            var packageId = new PACKAGE_ID
            {
                name = name,
                publisher = publisherDn
            };

            var packageFamilyNameLength = 0u;
            PackageFamilyNameFromId(packageId, ref packageFamilyNameLength, null);

            var packageFamilyNameBuilder = new StringBuilder((int)packageFamilyNameLength);
            if (PackageFamilyNameFromId(packageId, ref packageFamilyNameLength, packageFamilyNameBuilder) == WIN32_ERROR.ERROR_SUCCESS)
            {
                return packageFamilyNameBuilder.ToString();
            }
            throw new InvalidOperationException("Failed to retrieve package family name from package ID.");
        }
    }
}

