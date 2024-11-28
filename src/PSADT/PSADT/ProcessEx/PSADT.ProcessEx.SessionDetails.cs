using System;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Represents information about a user session.
    /// </summary>
    public class SessionDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionDetails"/> class with specified values.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="username">The username associated with the session.</param>
        public SessionDetails(uint sessionId, string username)
        {
            SessionId = sessionId;
            Username = username ?? throw new ArgumentNullException(nameof(username), "Username cannot be null.");
        }

        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        public uint SessionId { get; }

        /// <summary>
        /// Gets or sets the username associated with the session.
        /// </summary>
        public string Username { get; }
    }
}
