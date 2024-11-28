﻿using System;
using System.Collections;

namespace PSADT.Types
{
    /// <summary>
    /// Represents basic information about a process.
    /// </summary>
    public readonly struct ProcessObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessObject"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        public ProcessObject(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("Name", "A mandatory property was null or empty.");
            }
            Name = name;
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
                throw new ArgumentNullException("Name", "A mandatory property was null or empty.");
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
        public ProcessObject(Hashtable properties)
        {
            // Validate the provided name value.
            if (!properties.ContainsKey("Name"))
            {
                throw new ArgumentException("A mandatory property was not provided.", "Name");
            }
            if (string.IsNullOrWhiteSpace(properties["Name"]?.ToString()))
            {
                throw new ArgumentNullException("Name", "A mandatory property was null or empty.");
            }
            Name = properties["Name"]!.ToString()!;

            // Add in the description if it's valid.
            if (!string.IsNullOrWhiteSpace(properties["Description"]?.ToString()))
            {
                Description = properties["Description"]!.ToString();
            }
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        public string? Description { get; }

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
