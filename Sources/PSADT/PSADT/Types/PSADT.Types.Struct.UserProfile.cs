using System;

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
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null or empty.</exception>
        public UserProfile(string ntAccount, string sid, string profilePath)
        {
            NTAccount = !string.IsNullOrWhiteSpace(ntAccount) ? ntAccount : throw new ArgumentNullException(nameof(ntAccount), "NT account cannot be null or empty.");
            SID = !string.IsNullOrWhiteSpace(sid) ? sid : throw new ArgumentNullException(nameof(sid), "SID cannot be null or empty.");
            ProfilePath = !string.IsNullOrWhiteSpace(profilePath) ? profilePath : throw new ArgumentNullException(nameof(profilePath), "Profile path cannot be null or empty.");
        }

        /// <summary>
        /// Gets the NT account associated with the user profile.
        /// </summary>
        public string NTAccount { get; }

        /// <summary>
        /// Gets the security identifier (SID) for the user profile.
        /// </summary>
        public string SID { get; }

        /// <summary>
        /// Gets the path to the user's profile directory.
        /// </summary>
        public string ProfilePath { get; }

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
