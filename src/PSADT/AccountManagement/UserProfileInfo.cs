using System;
using System.Globalization;
using System.IO;
using System.Security.Principal;

namespace PSADT.AccountManagement
{
    /// <summary>
    /// Represents information about a user profile.
    /// </summary>
    public sealed record class UserProfileInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfileInfo"/> struct.
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
        /// <param name="userLocale">The locale information for the user.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null or empty.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
        public UserProfileInfo(
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
            DirectoryInfo? oneDriveCommercialPath = null,
            CultureInfo? userLocale = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ntAccount?.Value, nameof(ntAccount));
            ArgumentNullException.ThrowIfNull(profilePath);
            ArgumentNullException.ThrowIfNull(ntAccount);
            ArgumentNullException.ThrowIfNull(sid);
            NTAccount = ntAccount;
            SID = sid;
            ProfilePathValue = profilePath.FullName;
            AppDataPathValue = appDataPath?.FullName;
            LocalAppDataPathValue = localAppDataPath?.FullName;
            DesktopPathValue = desktopPath?.FullName;
            DocumentsPathValue = documentsPath?.FullName;
            StartMenuPathValue = startMenuPath?.FullName;
            TempPathValue = tempPath?.FullName;
            OneDrivePathValue = oneDrivePath?.FullName;
            OneDriveCommercialPathValue = oneDriveCommercialPath?.FullName;
            UserLocale = userLocale;
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
        /// <remarks>Rebuilt on each read from the path it was recorded under. A <see cref="DirectoryInfo"/> compares
        /// by reference, so holding one directly would make two descriptions of the same profile unequal - and these
        /// are compared, to tell whether the set of profiles on a machine has changed.</remarks>
        public DirectoryInfo ProfilePath => new(ProfilePathValue);
        /// <summary>
        /// Gets the path to the user's AppData directory.
        /// </summary>

        public DirectoryInfo? AppDataPath => AppDataPathValue is string appDataPath ? new(appDataPath) : null;

        /// <summary>
        /// Gets the path to the user's LocalAppData directory.
        /// </summary>
        public DirectoryInfo? LocalAppDataPath => LocalAppDataPathValue is string localAppDataPath ? new(localAppDataPath) : null;

        /// <summary>
        /// Gets the path to the user's Desktop directory.
        /// </summary>
        public DirectoryInfo? DesktopPath => DesktopPathValue is string desktopPath ? new(desktopPath) : null;

        /// <summary>
        /// Gets the path to the user's Documents directory.
        /// </summary>
        public DirectoryInfo? DocumentsPath => DocumentsPathValue is string documentsPath ? new(documentsPath) : null;

        /// <summary>
        /// Gets the path to the user's Start Menu directory.
        /// </summary>
        public DirectoryInfo? StartMenuPath => StartMenuPathValue is string startMenuPath ? new(startMenuPath) : null;

        /// <summary>
        /// Gets the path to the user's Temp directory.
        /// </summary>
        public DirectoryInfo? TempPath => TempPathValue is string tempPath ? new(tempPath) : null;

        /// <summary>
        /// Gets the path to the user's OneDrive directory.
        /// </summary>
        public DirectoryInfo? OneDrivePath => OneDrivePathValue is string oneDrivePath ? new(oneDrivePath) : null;

        /// <summary>
        /// Gets the path to the user's OneDrive for Business directory.
        /// </summary>
        public DirectoryInfo? OneDriveCommercialPath => OneDriveCommercialPathValue is string oneDriveCommercialPath ? new(oneDriveCommercialPath) : null;

        /// <summary>
        /// Gets the locale information for the user.
        /// </summary>
        public CultureInfo? UserLocale { get; }

        /// <summary>
        /// The path recorded for <see cref="ProfilePath"/>.
        /// </summary>
        private readonly string ProfilePathValue;

        /// <summary>
        /// The path recorded for <see cref="AppDataPath"/>.
        /// </summary>
        private readonly string? AppDataPathValue;

        /// <summary>
        /// The path recorded for <see cref="LocalAppDataPath"/>.
        /// </summary>
        private readonly string? LocalAppDataPathValue;

        /// <summary>
        /// The path recorded for <see cref="DesktopPath"/>.
        /// </summary>
        private readonly string? DesktopPathValue;

        /// <summary>
        /// The path recorded for <see cref="DocumentsPath"/>.
        /// </summary>
        private readonly string? DocumentsPathValue;

        /// <summary>
        /// The path recorded for <see cref="StartMenuPath"/>.
        /// </summary>
        private readonly string? StartMenuPathValue;

        /// <summary>
        /// The path recorded for <see cref="TempPath"/>.
        /// </summary>
        private readonly string? TempPathValue;

        /// <summary>
        /// The path recorded for <see cref="OneDrivePath"/>.
        /// </summary>
        private readonly string? OneDrivePathValue;

        /// <summary>
        /// The path recorded for <see cref="OneDriveCommercialPath"/>.
        /// </summary>
        private readonly string? OneDriveCommercialPathValue;
    }
}
