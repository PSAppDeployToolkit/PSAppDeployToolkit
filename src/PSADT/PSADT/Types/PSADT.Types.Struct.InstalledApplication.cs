using System;
using System.Globalization;

namespace PSADT.Types
{
    /// <summary>
    /// Represents an installed application and its related information.
    /// </summary>
    public readonly struct InstalledApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstalledApplication"/> struct.
        /// Converts the provided install date string to <see cref="DateTime"/> based on the system's culture.
        /// </summary>
        /// <param name="uninstallKey">The registry key that contains the uninstall entry.</param>
        /// <param name="uninstallParentKey">The registry key for the subkey's parent.</param>
        /// <param name="uninstallSubKey">The registry subkey for uninstalling the application.</param>
        /// <param name="productCode">The product code for the application.</param>
        /// <param name="displayName">The display name of the application.</param>
        /// <param name="displayVersion">The version of the application.</param>
        /// <param name="uninstallString">The uninstall string used to remove the application.</param>
        /// <param name="quietUninstallString">The quiet uninstall string used to remove the application.</param>
        /// <param name="installSource">The source from which the application was installed.</param>
        /// <param name="installLocation">The location where the application is installed.</param>
        /// <param name="installDate">The date the application was installed (as a string).</param>
        /// <param name="publisher">The publisher of the application.</param>
        /// <param name="systemComponent">A value indicating whether the application is a system component.</param>
        /// <param name="windowsInstaller">A value indicating whether the application is an MSI.</param>
        /// <param name="is64BitApplication">A value indicating whether the application is a 64-bit application.</param>
        public InstalledApplication(
            string uninstallKey,
            string uninstallParentKey,
            string uninstallSubKey,
            Guid? productCode,
            string displayName,
            string displayVersion,
            string uninstallString,
            string quietUninstallString,
            string installSource,
            string installLocation,
            string installDate,
            string publisher,
            bool systemComponent,
            bool windowsInstaller,
            bool is64BitApplication)
        {
            UninstallKey = uninstallKey;
            UninstallParentKey = uninstallParentKey;
            UninstallSubKey = uninstallSubKey;
            ProductCode = productCode;
            DisplayName = displayName;
            DisplayVersion = displayVersion;
            UninstallString = uninstallString;
            QuietUninstallString = quietUninstallString;
            InstallSource = installSource;
            InstallLocation = installLocation;
            Publisher = publisher;
            SystemComponent = systemComponent;
            WindowsInstaller = windowsInstaller;
            Is64BitApplication = is64BitApplication;

            DateTime parsedDate;
            // Attempt to parse the date based on yyyyMMdd format expected from Windows Installer
            if (!DateTime.TryParseExact(installDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsedDate))
            {
                // Attempt to parse the string date based on the current culture
                if (!DateTime.TryParse(installDate, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsedDate))
                {
                    // Fallback to smallest possible value of System.DateTime if parsing fails
                    parsedDate = DateTime.MinValue;
                }
            }
            InstallDate = parsedDate;
        }

        /// <summary>
        /// Gets the registry key that contains the uninstall entry.
        /// </summary>
        public string UninstallKey { get; }

        /// <summary>
        /// Gets the registry key for the subkey's parent.
        /// </summary>
        public string UninstallParentKey { get; }

        /// <summary>
        /// Gets the registry subkey for uninstalling the application.
        /// </summary>
        public string UninstallSubKey { get; }

        /// <summary>
        /// Gets the product code for the application.
        /// </summary>
        public Guid? ProductCode { get; }

        /// <summary>
        /// Gets the display name of the application.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the version of the application.
        /// </summary>
        public string DisplayVersion { get; }

        /// <summary>
        /// Gets the uninstall string used to remove the application.
        /// </summary>
        public string UninstallString { get; }

        /// <summary>
        /// Gets the quiet uninstall string used to remove the application.
        /// </summary>
        public string QuietUninstallString { get; }

        /// <summary>
        /// Gets the source from which the application was installed.
        /// </summary>
        public string InstallSource { get; }

        /// <summary>
        /// Gets the location where the application is installed.
        /// </summary>
        public string InstallLocation { get; }

        /// <summary>
        /// Gets the date the application was installed as a <see cref="DateTime"/> object.
        /// </summary>
        public DateTime InstallDate { get; }

        /// <summary>
        /// Gets the publisher of the application.
        /// </summary>
        public string Publisher { get; }

        /// <summary>
        /// Gets a value indicating whether the application is a system component.
        /// </summary>
        public bool SystemComponent { get; }

        /// <summary>
        /// Gets a value indicating whether the application is an MSI.
        /// </summary>
        public bool WindowsInstaller { get; }

        /// <summary>
        /// Gets a value indicating whether the application is a 64-bit application.
        /// </summary>
        public bool Is64BitApplication { get; }

        /// <summary>
        /// Returns a string representation of the installed application.
        /// </summary>
        /// <returns>A string that contains key details about the installed application.</returns>
        public override string ToString()
        {
            return $"Installed Application: {DisplayName} (Version: {DisplayVersion}, Publisher: {Publisher})";
        }

        /// <summary>
        /// Validates whether the product code is a valid GUID.
        /// </summary>
        /// <returns>True if the product code is a valid GUID; otherwise, false.</returns>
        public bool IsValidProductCode()
        {
            return (null != ProductCode);
        }
    }
}
