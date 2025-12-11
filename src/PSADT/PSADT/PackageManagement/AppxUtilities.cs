using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Xml;

namespace PSADT.PackageManagement
{
    /// <summary>
    /// Methods to interact with Appx packages based on identifier
    /// </summary>
    public static class AppxUtilities
    {
        private const string PROVISIONED_PACKAGE_SUBKEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications";

        private const string PACKAGE_SUBKEY = @"Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";

        private const string USER_PACKAGE_SUBKEY = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";

        private const int ERROR_SUCCESS = 0;

        [DllImport("kernelbase.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern ulong VerifyPackageFamilyName(string packageFamilyName);

        [DllImport("kernelbase.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern ulong VerifyPackageFullName(string packageFullName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern ulong PackageFamilyNameFromId(
            PACKAGE_ID packageId,
            ref uint packageFamilyNameLength,
            StringBuilder? packageFamilyName);

        [DllImport("kernelbase.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern ulong PackageFamilyNameFromFullName(
            string packageFullName,
            ref uint packageFamilyNameLength,
            StringBuilder? packageFamilyName);

        [DllImport("kernelbase.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern ulong PackageNameAndPublisherIdFromFamilyName(
            string packageFamilyName,
            ref uint packageNameLength,
            StringBuilder? packageName,
            ref uint packagePublisherIdLength,
            StringBuilder? packagePublisherId);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
        private struct PACKAGE_ID
        {
            public uint reserved;
            public ProcessorArchitecture processorArchitecture;
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
        /// Check if the identifier is a valid Appx package family name.
        /// </summary>
        public static bool IsValidFamilyName(string packageFamilyName)
        {
            return VerifyPackageFamilyName(packageFamilyName) == ERROR_SUCCESS;
        }

        /// <summary>
        /// Check if the identifier is a valid Appx package full name.
        /// </summary>
        public static bool IsValidFullName(string packageFullName)
        {
            return VerifyPackageFullName(packageFullName) == ERROR_SUCCESS;
        }

        /// <summary>
        /// Check if the identifier is a valid Appx package identifier (either family name or full name).
        /// </summary>
        public static bool IsValidIdentifier(string identifier)
        {
            return IsValidFamilyName(identifier) || IsValidFullName(identifier);
        }

        /// <summary>
        /// Get all provisioned package identifiers from the registry.
        /// </summary>
        public static ReadOnlyCollection<string> GetProvisionedPackageIdentifiers()
        {

            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var packageKey = baseKey.OpenSubKey(PROVISIONED_PACKAGE_SUBKEY);
            return new ReadOnlyCollection<string>(packageKey == null ? Array.Empty<string>() : packageKey.GetSubKeyNames());
        }

        /// <summary>
        /// Get all package identifiers from the registry.
        /// </summary>
        public static ReadOnlyCollection<string> GetPackageIdentifiers(IdentityReference? user = null)
        {
            if (user != null)
            {
                var sid = user.Translate(typeof(SecurityIdentifier)).Value;
                using var userKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
                using var key = userKey.OpenSubKey($@"{sid}\{USER_PACKAGE_SUBKEY}");
                return new ReadOnlyCollection<string>(key == null ? Array.Empty<string>() : key.GetSubKeyNames());
            }
            else
            {
                using var key = Registry.ClassesRoot.OpenSubKey(PACKAGE_SUBKEY);
                return new ReadOnlyCollection<string>(key == null ? Array.Empty<string>() : key.GetSubKeyNames());
            }
        }

        /// <summary>
        /// Retrieve the package family name from a given identifier.
        /// </summary>
        public static string GetFamilyFromFullName(string identifier)
        {
            if (!IsValidIdentifier(identifier))
            {
                throw new ArgumentException("Invalid package identifier.", nameof(identifier));
            }

            var parts = identifier.Split('_');
            return $"{parts.First()}_{parts.Last()}";
        }

        /// <summary>
        /// Retrieve all provisioned packages matching the specified package family.
        /// </summary>
        public static ReadOnlyCollection<string> GetProvisionedPackageFamilyMembers(string packageFamilyName)
        {
            if (!IsValidFamilyName(packageFamilyName))
            {
                throw new ArgumentException("Invalid package family name.", nameof(packageFamilyName));
            }

            return new ReadOnlyCollection<string>(
                GetProvisionedPackageIdentifiers()
                    .Where(id => GetFamilyFromFullName(id).Equals(packageFamilyName, StringComparison.InvariantCultureIgnoreCase))
                    .ToList()
            );
        }

        /// <summary>
        /// Retrieve all packages matching the specified package family.
        /// </summary>
        public static ReadOnlyCollection<string> GetPackageFamilyMembers(string packageFamilyName)
        {
            if (!IsValidFamilyName(packageFamilyName))
            {
                throw new ArgumentException("Invalid package family name.", nameof(packageFamilyName));
            }

            return new ReadOnlyCollection<string>(
                GetPackageIdentifiers()
                    .Where(id => GetFamilyFromFullName(id).Equals(packageFamilyName, StringComparison.OrdinalIgnoreCase))
                    .ToList()
            );
        }

        /// <summary>
        /// Checks if the given identifier is present in the provisioned packages.
        /// </summary>
        public static bool IsProvisionedPackageInstalled(string packageFullName)
        {
            if (!IsValidFullName(packageFullName))
            {
                throw new ArgumentException("Invalid package full name.", nameof(packageFullName));
            }

            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var packageKey = baseKey.OpenSubKey($"{PROVISIONED_PACKAGE_SUBKEY}\\{packageFullName}");
            return packageKey != null;
        }

        /// <summary>
        /// Checks if the given identifier is present in the installed packages.
        /// </summary>
        public static bool IsPackageInstalled(string packageFullName, IdentityReference? user = null)
        {
            if (!IsValidFullName(packageFullName))
            {
                throw new ArgumentException("Invalid package full name.", nameof(packageFullName));
            }

            if (user != null)
            {
                var sid = user.Translate(typeof(SecurityIdentifier)).Value;
                using var userKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
                using var key = userKey.OpenSubKey($@"{sid}\{USER_PACKAGE_SUBKEY}\\{packageFullName}");
                return key != null;
            }
            else
            {
                using var key = Registry.ClassesRoot.OpenSubKey($"{PACKAGE_SUBKEY}\\{packageFullName}");
                return key != null;
            }
        }

        /// <summary>
        /// Get the manifest for a package.
        /// </summary>
        public static AppxManifest GetPackageManifest(string packageFullName)
        {
            using var packageKey = Registry.ClassesRoot.OpenSubKey($"{PACKAGE_SUBKEY}\\{packageFullName}")
                ?? throw new ArgumentException($"Package identifier '{packageFullName}' not found in installed packages.", nameof(packageFullName));

            var root = packageKey.GetValue("PackageRootFolder") as string
                ?? throw new InvalidOperationException("The package does not contain information about its manifest.");

            return ReadPackageManifest(Path.Combine(root, "AppxManifest.xml"));
        }

        /// <summary>
        /// Get the manifest for a package.
        /// </summary>
        public static AppxManifest GetProvisionedPackageManifest(string packageFullName)
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var packageKey = baseKey.OpenSubKey($"{PROVISIONED_PACKAGE_SUBKEY}\\{packageFullName}")
                ?? throw new ArgumentException($"Package identifier '{packageFullName}' not found in installed packages.", nameof(packageFullName));

            var manifestPath = packageKey.GetValue("Path") as string
                ?? throw new InvalidOperationException("The package does not contain information about its manifest.");
            manifestPath = Environment.ExpandEnvironmentVariables(manifestPath);

            return manifestPath.EndsWith("AppxBundleManifest.xml", StringComparison.OrdinalIgnoreCase)
                ? ReadBundleManifest(manifestPath)
                : ReadPackageManifest(manifestPath);
        }

        /// <summary>
        /// Retrieves the package family name from a given package name and publisher.
        /// </summary>
        public static string GetPackageFamilyName(string name, string publisher)
        {
            var packageId = new PACKAGE_ID
            {
                name = name,
                publisher = publisher
            };

            var packageFamilyNameLength = 0u;
            PackageFamilyNameFromId(packageId, ref packageFamilyNameLength, null);

            var packageFamilyNameBuilder = new StringBuilder((int)packageFamilyNameLength);
            if (PackageFamilyNameFromId(packageId, ref packageFamilyNameLength, packageFamilyNameBuilder) == ERROR_SUCCESS)
            {
                return packageFamilyNameBuilder.ToString();
            }
            throw new InvalidOperationException("Failed to retrieve package family name from package ID.");
        }

        /// <summary>
        /// Retrieves relevant data from a package manifest XML document.
        /// </summary>
        private static AppxManifest ReadPackageManifest(string manifestPath)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(manifestPath);

            var packageIdentityNode = xmlDoc.SelectSingleNode("/*[local-name()='Package']/*[local-name()='Identity']")
                ?? throw new InvalidOperationException("No valid Package Identity node found in the manifest.");

            var name = packageIdentityNode?.Attributes?["Name"]?.Value
                ?? throw new InvalidOperationException("No valid Name attribute found in the Package Identity node.");
            var publisher = packageIdentityNode?.Attributes?["Publisher"]?.Value
                ?? throw new InvalidOperationException("No valid Publisher attribute found in the Package Identity node.");
            var version = packageIdentityNode?.Attributes?["Version"]?.Value
                ?? throw new InvalidOperationException("No valid Version attribute found in the Package Identity node.");
            var architecture = packageIdentityNode?.Attributes?["ProcessorArchitecture"]?.Value
                ?? ProcessorArchitecture.Neutral.ToString();

            var resourceId = packageIdentityNode?.Attributes?["ResourceId"]?.Value ?? string.Empty;
            var familyIdentifer = GetPackageFamilyName(name, publisher);
            var publisherId = familyIdentifer.Split('_').Last();
            var fullNameIdentifier = $"{name}_{version}_{architecture}_{resourceId}_{publisherId}";

            return new AppxManifest(
                name,
                publisher,
                version,
                (ProcessorArchitecture)Enum.Parse(typeof(ProcessorArchitecture), architecture, true),
                resourceId,
                publisherId,
                fullNameIdentifier,
                familyIdentifer,
                manifestPath,
                false
            );
        }


        /// <summary>
        /// Retrieves relevant data from a bundle manifest XML document.
        /// </summary>
        private static AppxManifest ReadBundleManifest(string manifestPath)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(manifestPath);

            var bundleIdentityNode = xmlDoc.SelectSingleNode("/*[local-name()='Bundle']/*[local-name()='Identity']")
                ?? throw new InvalidOperationException("No valid Bundle Identity node found in the manifest.");

            var bundleAttr = bundleIdentityNode.Attributes
                ?? throw new InvalidOperationException("The bundle indentity node is missing required attributes");

            var name = bundleAttr["Name"]?.Value
                ?? throw new InvalidOperationException("No valid Name attribute found in the Bundle Identity node.");
            var publisher = bundleAttr["Publisher"]?.Value
                ?? throw new InvalidOperationException("No valid Publisher attribute found in the Bundle Identity node.");
            var version = bundleAttr["Version"]?.Value
                ?? throw new InvalidOperationException("No valid Version attribute found in the Bundle Identity node.");
            var architecture = ProcessorArchitecture.Neutral;
            var resourceId = "~";

            var familyIdentifer = GetPackageFamilyName(name, publisher);
            var publisherId = familyIdentifer.Split('_').Last();
            var fullNameIdentifier = $"{name}_{version}_{architecture.ToString().ToLower()}_{resourceId}_{publisherId}";

            var applicationNodes = xmlDoc.SelectNodes("/*[local-name()='Bundle']/*[local-name()='Packages']/*[@Type='application']");
            var resourceNodes = xmlDoc.SelectNodes("/*[local-name()='Bundle']/*[local-name()='Packages']/*[@Type='resource']");

            var bundledApplications = applicationNodes != null && applicationNodes.Count > 0
                ? applicationNodes.Cast<XmlNode>().Select(node => $"{name}_{node.Attributes?["Version"]?.Value}_{node.Attributes?["Architecture"]?.Value}_{node.Attributes?["ResourceId"]?.Value}_{publisherId}").ToArray()
                : Array.Empty<string>();

            var bundledResources = resourceNodes != null && resourceNodes.Count > 0
                ? resourceNodes.Cast<XmlNode>().Select(node => $"{name}_{node.Attributes?["Version"]?.Value}_{node.Attributes?["Architecture"]?.Value}_{node.Attributes?["ResourceId"]?.Value}_{publisherId}").ToArray()
                : Array.Empty<string>();

            return new AppxManifest(
                name,
                publisher,
                version,
                architecture,
                resourceId,
                publisherId,
                fullNameIdentifier,
                familyIdentifer,
                manifestPath,
                true,
                new ReadOnlyCollection<string>(bundledApplications),
                new ReadOnlyCollection<string>(bundledResources)
            );
        }
    }
}

