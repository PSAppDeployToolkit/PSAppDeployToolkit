using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSADT.Utilities
{
    /// <summary>
    /// Methods to interact with Appx packages based on identifier
    /// </summary>
    public static class PackageUtilities
    {
        private const int ERROR_SUCCESS = 0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1838:Avoid 'StringBuilder' parameters for P/Invokes", Justification = "This P/Invoke is temporary for now.")]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern ulong PackageFamilyNameFromId(
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

        [StructLayout(LayoutKind.Explicit)]
        private struct PACKAGE_VERSION
        {
            [FieldOffset(0)]
            public ulong Version;
            [FieldOffset(0)]
            public ushort Revision;
            [FieldOffset(2)]
            public ushort Build;
            [FieldOffset(4)]
            public ushort Minor;
            [FieldOffset(6)]
            public ushort Major;
        }

        /// <summary>
        /// Retrieves the package family name from a given package name and publisher.
        /// </summary>
        public static string GetPackageFamilyName(string name, string publisher)
        {
            PACKAGE_ID packageId = new()
            {
                name = name,
                publisher = publisher
            };

            uint packageFamilyNameLength = 0;
            _ = PackageFamilyNameFromId(packageId, ref packageFamilyNameLength, null);

            StringBuilder packageFamilyNameBuilder = new((int)packageFamilyNameLength);
            return PackageFamilyNameFromId(packageId, ref packageFamilyNameLength, packageFamilyNameBuilder) == ERROR_SUCCESS
                ? packageFamilyNameBuilder.ToString()
                : throw new InvalidOperationException("Failed to retrieve package family name from package ID.");
        }
    }
}
