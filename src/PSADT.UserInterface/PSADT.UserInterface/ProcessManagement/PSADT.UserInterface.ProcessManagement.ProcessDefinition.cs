using System;
using System.Collections;

namespace PSADT.UserInterface.ProcessManagement
{
    /// <summary>
    /// Represents basic information about a process.
    /// </summary>
    public sealed class ProcessDefinition
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
        /// Initializes a new instance of the <see cref="ProcessDefinition"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        /// <param name="filter">A filter for the specified process.</param>
        public ProcessDefinition(string name, Func<RunningProcess, bool> filter)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("Name value is null or invalid.", (Exception?)null);
            }
            Name = name;

            if (null == filter)
            {
                throw new ArgumentNullException("Filter value is null or invalid.", (Exception?)null);
            }
            Filter = filter;
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

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentNullException("Description value is null or invalid.", (Exception?)null);
            }
            Description = description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        /// <param name="description">The description of the process.</param>
        /// <param name="filter">A filter for the specified process.</param>
        public ProcessDefinition(string name, string description, Func<RunningProcess, bool> filter)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("Name value is null or invalid.", (Exception?)null);
            }
            Name = name;

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentNullException("Description value is null or invalid.", (Exception?)null);
            }
            Description = description;

            if (null == filter)
            {
                throw new ArgumentNullException("Filter value is null or invalid.", (Exception?)null);
            }
            Filter = filter;
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
            if (properties.ContainsKey("Description"))
            {
                if (properties["Description"] is not string description || string.IsNullOrWhiteSpace(description))
                {
                    throw new ArgumentOutOfRangeException("Description value is not valid.", (Exception?)null);
                }
                Description = description;
            }
            if (properties.ContainsKey("Filter"))
            {
                if (properties["Filter"] is not Func<RunningProcess, bool> filter)
                {
                    throw new ArgumentOutOfRangeException("filter value is not valid.", (Exception?)null);
                }
                Filter = filter;
            }

            // The hashtable was correctly defined, assign the remaining values.
            Name = name;
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
        public readonly Func<RunningProcess, bool>? Filter;

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
