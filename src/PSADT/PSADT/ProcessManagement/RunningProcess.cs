using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a running process.
    /// </summary>
    public sealed record RunningProcess
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunningProcess"/> class with specified properties.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="description"></param>
        /// <param name="fileName"></param>
        /// <param name="argumentList"></param>
        /// <param name="username"></param>
        internal RunningProcess(Process process, string description, string fileName, IEnumerable<string> argumentList, NTAccount? username)
        {
            Process = process ?? throw new ArgumentNullException("Process cannot be null.", (Exception?)null);
            Description = !string.IsNullOrWhiteSpace(description) ? description : throw new ArgumentNullException("Description cannot be null or empty.", (Exception?)null);
            FileName = !string.IsNullOrWhiteSpace(fileName) ? fileName : throw new ArgumentNullException("FileName cannot be null or empty.", (Exception?)null);
            ArgumentList = new ReadOnlyCollection<string>(argumentList.Where(static a => !string.IsNullOrWhiteSpace(a)).ToArray());
            if (username is not null)
            {
                Username = username;
            }
        }

        /// <summary>
        /// Gets the process associated with the running process.
        /// </summary>
        public readonly Process Process;

        /// <summary>
        /// Gets the description of the running process.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// Gets the file path of the running process.
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// Gets the arguments passed to the running process.
        /// </summary>
        public readonly IReadOnlyList<string> ArgumentList;

        /// <summary>
        /// Represents the username associated with a Windows NT account.
        /// </summary>
        /// <remarks>The <see cref="NTAccount"/> class provides a way to work with Windows NT account
        /// names, including translating them to and from security identifiers (SIDs). This field is
        /// read-only.</remarks>
        public readonly NTAccount? Username;
    }
}
