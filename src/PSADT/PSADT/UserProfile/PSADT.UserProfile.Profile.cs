namespace PSADT.UserProfile
{
    /// <summary>
    /// Represents user profile information, including account, domain, username, SID, and profile paths.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// Gets or sets the NT account associated with the user profile.
        /// </summary>
        public string NTAccount { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the NT domain associated with the user profile.
        /// </summary>
        public string NTDomain { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username associated with the user profile.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SID associated with the user profile.
        /// </summary>
        public string Sid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the profile path for the user profile.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the user's Documents folder.
        /// </summary>
        public string DocumentsPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the user's Desktop folder.
        /// </summary>
        public string DesktopPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the user's Pictures folder.
        /// </summary>
        public string PicturesPath { get; set; } = string.Empty;
    }
}
