using System;
using System.IO;
using System.Security.Principal;

namespace PSADT.Types
{
    /// <summary>
    /// Represents information about a user profile.
    /// </summary>
    public readonly struct UserProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfile"/> struct.
        /// </summary>
        /// <param name="ntAccount">The NT account associated with the user profile.</param>
        /// <param name="sid">The security identifier (SID) for the user profile.</param>
        /// <param name="profilePath">The path to the user's profile directory.</param>
        /// <param name="appDataPath">The path to the user's AppData directory.</param>
        /// <param name="localAppDataPath">The path to the user's LocalAppData directory.</param>
        /// <param name="desktopPath">The path to the user's Desktop directory.</param>
        /// <param name="documentsPath">The path to the user's Documents directory.</param>
        /// <param name="startMenuPath">The path to the user's Start Menu directory.</param>
        /// <param name="tempPath">The path to the user's Temp directory.</param>
        /// <param name="oneDrivePath">The path to the user's OneDrive directory.</param>
        /// <param name="oneDriveCommercialPath">The path to the user's OneDrive for Business directory.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null or empty.</exception>
        public UserProfile(
            NTAccount ntAccount,
            SecurityIdentifier sid,
            DirectoryInfo profilePath,
            DirectoryInfo? appDataPath = null,
            DirectoryInfo? localAppDataPath = null,
            DirectoryInfo? desktopPath = null,
            DirectoryInfo? documentsPath = null,
            DirectoryInfo? startMenuPath = null,
            DirectoryInfo? tempPath = null,
            DirectoryInfo? oneDrivePath = null,
            DirectoryInfo? oneDriveCommercialPath = null)
        {
            NTAccount = ntAccount;
            SID = sid;
            ProfilePath = profilePath;
            AppDataPath = appDataPath;
            LocalAppDataPath = localAppDataPath;
            DesktopPath = desktopPath;
            DocumentsPath = documentsPath;
            StartMenuPath = startMenuPath;
            TempPath = tempPath;
            OneDrivePath = oneDrivePath;
            OneDriveCommercialPath = oneDriveCommercialPath;
        }

        /// <summary>
        /// Gets the NT account associated with the user profile.
        /// </summary>
        public NTAccount NTAccount { get; }

        /// <summary>
        /// Gets the security identifier (SID) for the user profile.
        /// </summary>
        public SecurityIdentifier SID { get; }

        /// <summary>
        /// Gets the path to the user's profile directory.
        /// </summary>
        public DirectoryInfo ProfilePath { get; }
        /// <summary>
        /// Gets the path to the user's AppData directory.
        /// </summary>

        public DirectoryInfo? AppDataPath { get; }

        /// <summary>
        /// Gets the path to the user's LocalAppData directory.
        /// </summary>
        public DirectoryInfo? LocalAppDataPath { get; }

        /// <summary>
        /// Gets the path to the user's Desktop directory.
        /// </summary>
        public DirectoryInfo? DesktopPath { get; }

        /// <summary>
        /// Gets the path to the user's Documents directory.
        /// </summary>
        public DirectoryInfo? DocumentsPath { get; }

        /// <summary>
        /// Gets the path to the user's Start Menu directory.
        /// </summary>
        public DirectoryInfo? StartMenuPath { get; }

        /// <summary>
        /// Gets the path to the user's Temp directory.
        /// </summary>
        public DirectoryInfo? TempPath { get; }

        /// <summary>
        /// Gets the path to the user's OneDrive directory.
        /// </summary>
        public DirectoryInfo? OneDrivePath { get; }

        /// <summary>
        /// Gets the path to the user's OneDrive for Business directory.
        /// </summary>
        public DirectoryInfo? OneDriveCommercialPath { get; }

        /// <summary>
        /// Returns a string that represents the current <see cref="UserProfile"/> object.
        /// </summary>
        /// <returns>A string that represents the user profile information.</returns>
        public override string ToString()
        {
            return $"NT Account: [{NTAccount}], SID: [{SID}], Profile Path: [{ProfilePath}].";
        }
    }
}
