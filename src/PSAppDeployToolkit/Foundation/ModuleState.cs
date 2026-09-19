using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Represents the state of a module, including its environment, language, configuration, strings, deployment sessions, and last exit code.
    /// </summary>
    public sealed class ModuleState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleState"/> class.
        /// </summary>
        /// <param name="scriptDirectories">The list of script directories for the module.</param>
        /// <param name="configDirectories">The list of config directories for the module.</param>
        /// <param name="stringDirectories">The list of string directories for the module.</param>
        /// <param name="environment">The environment table for the module.</param>
        /// <param name="config">The configuration hashtable for the module.</param>
        /// <param name="language">The culture info for the module.</param>
        /// <param name="strings">The strings hashtable for the module.</param>
        /// <param name="initStartDateTime">The initialization start date and time for the module.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        /// <exception cref="ArgumentException">Thrown when the configuration or strings hashtable is empty.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S6561:Avoid using \"DateTime.Now\" for benchmarking or timing operations", Justification = "This is OK, we don't need nanosecond precision..")]
        public ModuleState(ReadOnlyCollection<DirectoryInfo> scriptDirectories, ReadOnlyCollection<DirectoryInfo>? configDirectories, ReadOnlyCollection<DirectoryInfo>? stringDirectories, EnvironmentTable environment, Hashtable config, CultureInfo language, Hashtable strings, DateTime initStartDateTime)
        {
            Directories = new(scriptDirectories, configDirectories, stringDirectories);
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Language = language ?? throw new ArgumentNullException(nameof(language));
            Strings = strings ?? throw new ArgumentNullException(nameof(strings));
            InitDuration = DateTime.Now - initStartDateTime;
            if (config.Count is 0)
            {
                throw new ArgumentException("Configuration hashtable cannot be empty.", nameof(config));
            }
            if (strings.Count is 0)
            {
                throw new ArgumentException("Strings hashtable cannot be empty.", nameof(strings));
            }
        }

        /// <summary>
        /// Gets the directories for the module, including script, config, and strings directories.
        /// </summary>
        public ModuleDirectories Directories { get; }

        /// <summary>
        /// Gets the environment table for the module.
        /// </summary>
        public EnvironmentTable Environment { get; }

        /// <summary>
        /// Gets the configuration hashtable for the module.
        /// </summary>
        public IDictionary Config { get; }

        /// <summary>
        /// Gets the culture info for the module.
        /// </summary>
        public CultureInfo Language { get; }

        /// <summary>
        /// Gets the strings hashtable for the module.
        /// </summary>
        public IDictionary Strings { get; }

        /// <summary>
        /// Gets the list of deployment sessions for the module.
        /// </summary>
        public IList<DeploymentSession> Sessions { get; } = [];

        /// <summary>
        /// Gets the initialization start date and time for the module.
        /// </summary>
        public TimeSpan InitDuration { get; }

        /// <summary>
        /// Gets or sets the countdown value for restarting the module on exit.
        /// </summary>
        public uint? RestartOnExitCountdown { get; set; }

        /// <summary>
        /// Gets or sets the reason text for shutting down the module.
        /// </summary>
        public string? ShutdownReasonText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to force close applications during shutdown.
        /// </summary>
        public bool? ShutdownNoForceCloseApps { get; set; }

        /// <summary>
        /// Gets or sets the last exit code for the module.
        /// </summary>
        public int? LastExitCode { get; set; }
    }
}
