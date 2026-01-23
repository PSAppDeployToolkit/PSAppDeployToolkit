using System;
using System.Collections;
using Newtonsoft.Json;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents basic information about a process.
    /// </summary>
    public sealed record ProcessDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDefinition"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        public ProcessDefinition(string name)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException("Name value is null or invalid.", (Exception?)null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDefinition"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        /// <param name="description">The description of the process.</param>
        [JsonConstructor]
        public ProcessDefinition(string name, string description) : this(name)
        {
            Description = !string.IsNullOrWhiteSpace(description) ? description : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDefinition"/> struct.
        /// </summary>
        /// <param name="properties">The hashtable with a process's name, and optionally a description.</param>
        public ProcessDefinition(Hashtable properties) : this((properties ?? throw new ArgumentNullException(nameof(properties)))["Name"] is string name ? name : string.Empty, properties["Description"] is string description ? description : string.Empty)
        {
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        [JsonProperty]
        public string Name { get; }

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        [JsonProperty]
        public string? Description { get; }

        /// <summary>
        /// Gets the filter script for the process.
        /// </summary>
        public Func<RunningProcessInfo, bool>? Filter { get; }
    }
}
