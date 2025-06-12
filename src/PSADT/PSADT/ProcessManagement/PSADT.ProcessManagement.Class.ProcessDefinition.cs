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
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("Name value is null or invalid.", (Exception?)null);
            }
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        /// <param name="description">The description of the process.</param>
        public ProcessDefinition(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("Name value is null or invalid.", (Exception?)null);
            }
            Name = name;

            if (!string.IsNullOrWhiteSpace(description))
            {
                Description = description;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="properties">The hashtable with a process's name, and optionally a description.</param>
        public ProcessDefinition(Hashtable properties)
        {
            // Nothing here is allowed to be null.
            if (properties["Name"] is not string name || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("Name value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (properties["Description"] is string description && !string.IsNullOrWhiteSpace(description))
            {
                Description = description;
            }

            // The hashtable was correctly defined, assign the remaining values.
            Name = name;
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        [DataMember]
        public readonly string? Description;

        /// <summary>
        /// Gets the filter script for the process.
        /// </summary>
        [DataMember]
        public readonly Func<RunningProcess, bool>? Filter;
    }
}
