using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Represents the directories for a module, including script, config, and strings directories.
    /// </summary>
    public sealed class ModuleDirectories
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleDirectories"/> class.
        /// </summary>
        /// <param name="script">The list of script directories for the module.</param>
        /// <param name="config">The list of config directories for the module.</param>
        /// <param name="strings">The list of string directories for the module.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="script"/> parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when any of the directory paths are null or whitespace.</exception>
        internal ModuleDirectories(IEnumerable<DirectoryInfo> script, IEnumerable<DirectoryInfo>? config, IEnumerable<DirectoryInfo>? strings)
        {
            Script = new ReadOnlyCollection<DirectoryInfo>([.. script ?? throw new ArgumentNullException(nameof(script))]);
            Config = new ReadOnlyCollection<DirectoryInfo>(config is not null ? [.. config] : []);
            Strings = new ReadOnlyCollection<DirectoryInfo>(strings is not null ? [.. strings] : []);
        }

        /// <summary>
        /// Gets the list of script directories for the module.
        /// </summary>
        public IReadOnlyList<DirectoryInfo> Script { get; }

        /// <summary>
        /// Gets the list of config directories for the module.
        /// </summary>
        public IReadOnlyList<DirectoryInfo> Config { get; }

        /// <summary>
        /// Gets the list of string directories for the module.
        /// </summary>
        public IReadOnlyList<DirectoryInfo> Strings { get; }
    }
}
