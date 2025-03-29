using System;
using System.Collections;
using System.Management.Automation;

namespace PSADT.Types
{
    /// <summary>
    /// Represents basic information about a process.
    /// </summary>
    public sealed class ProcessObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        public ProcessObject(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), "A mandatory property was null or empty.");
            }
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        /// <param name="filter">A filter for the specified process.</param>
        public ProcessObject(string name, ScriptBlock filter)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), "A mandatory property was null or empty.");
            }
            Name = name;
            Filter = filter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        /// <param name="description">The description of the process.</param>
        public ProcessObject(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), "A mandatory property was null or empty.");
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
        /// <param name="name">The name of the process.</param>
        /// <param name="description">The description of the process.</param>
        /// <param name="filter">A filter for the specified process.</param>
        public ProcessObject(string name, string description, ScriptBlock filter)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), "A mandatory property was null or empty.");
            }
            Name = name;

            if (!string.IsNullOrWhiteSpace(description))
            {
                Description = description;
            }
            Filter = filter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="properties">The hashtable with a process's name, and optionally a description.</param>
        public ProcessObject(Hashtable properties)
        {
            // Validate the provided name value.
            if (!properties.ContainsKey("Name"))
            {
                throw new ArgumentException("A mandatory property was not provided.", $"{nameof(properties)}.Name");
            }
            if (string.IsNullOrWhiteSpace((string?)(properties[nameof(Name)])))
            {
                throw new ArgumentNullException($"{nameof(properties)}.Name", "A mandatory property was null or empty.");
            }
            Name = (string)properties[nameof(Name)]!;

            // Add in the description if it's valid.
            if (!string.IsNullOrWhiteSpace((string?)properties[nameof(Description)]))
            {
                Description = (string)properties[nameof(Description)]!;
            }

            // Add in the filter if it's valid.
            if (!string.IsNullOrWhiteSpace(properties[nameof(Filter)]?.ToString()))
            {
                Filter = (ScriptBlock)properties[nameof(Filter)]!;
            }
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        public readonly string? Description;

        /// <summary>
        /// Gets the filter script for the process.
        /// </summary>
        public readonly ScriptBlock? Filter;

        /// <summary>
        /// Returns a string that represents the current <see cref="ProcessObject"/> object.
        /// </summary>
        /// <returns>A formatted string containing the name and description of the process.</returns>
        public override string ToString()
        {
            return $"Process Name: {Name}, Description: {Description}";
        }
    }
}
