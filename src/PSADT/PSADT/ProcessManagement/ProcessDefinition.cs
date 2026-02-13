using System;
using System.Collections;
using System.Runtime.Serialization;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents basic information about a process.
    /// </summary>
    [DataContract]
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
        public ProcessDefinition(string name, string description) : this(name)
        {
            Description = !string.IsNullOrWhiteSpace(description) ? description : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDefinition"/> struct.
        /// </summary>
        /// <param name="properties">The hashtable with a process's name, and optionally a description.</param>
        public ProcessDefinition(IDictionary properties) : this((properties ?? throw new ArgumentNullException(nameof(properties)))["Name"] as string ?? string.Empty, properties["Description"] as string ?? string.Empty)
        {
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string? Description;

        /// <summary>
        /// Gets the filter script for the process.
        /// </summary>
        [IgnoreDataMember]
        public Func<RunningProcessInfo, bool>? Filter { get; }
    }
}
