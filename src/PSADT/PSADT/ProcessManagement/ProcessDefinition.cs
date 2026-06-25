using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

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
            // Set name property first and foremost.
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (WildcardOnlyRegex.IsMatch(name))
            {
                throw new ArgumentException("The process name cannot be only wildcard characters.", nameof(name));
            }
            Name = name;

            // Set all calculated fields based on the name.
            SetCalculatedFields();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDefinition"/> struct.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        /// <param name="description">The description of the process.</param>
        public ProcessDefinition(string name, string? description) : this(name)
        {
            if (description?.Length > 0)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(description);
            }
            if (!string.IsNullOrWhiteSpace(description))
            {
                Description = description;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDefinition"/> struct.
        /// </summary>
        /// <param name="properties">The hashtable with a process's name, and optionally a description.</param>
        public ProcessDefinition(IDictionary properties) : this((string?)(properties ?? throw new ArgumentNullException(nameof(properties)))["Name"] ?? throw new ArgumentNullException(nameof(properties), "The specified key 'Name' is missing."), (string?)properties["Description"])
        {
        }

        /// <summary>
        /// Sets all calculated fields based on the name.
        /// </summary>
        private void SetCalculatedFields()
        {
            if (NameIsFullyQualifiedPath())
            {
                ProcessName = Path.GetFileNameWithoutExtension(Name);
            }
            if (Name.Contains('*', StringComparison.Ordinal))
            {
                NameRegex = new($"^{Regex.Escape(Name).Replace("\\*", ".*", StringComparison.Ordinal)}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (ProcessName is not null)
                {
                    ProcessNameRegex = new($"^{Regex.Escape(ProcessName).Replace("\\*", ".*", StringComparison.Ordinal)}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
            }
        }

        /// <summary>
        /// Sets all calculated fields after deserialization.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SetCalculatedFields();
        }

        /// <summary>
        /// Determines whether the process definition's name is a fully qualified path, which can be used to distinguish between process definitions that specify a process name with or without a path component.
        /// </summary>
        /// <returns><see langword="true"/> if the process definition's name is a fully qualified path; otherwise, <see langword="false"/>.</returns>
        public bool NameIsFullyQualifiedPath()
        {
            return Path.IsPathFullyQualified(Name);
        }

        /// <summary>
        /// Determines whether the specified input matches the process definition's name, taking into account potential wildcard characters in the name and performing a case-insensitive comparison.
        /// </summary>
        /// <param name="input">The input string to compare against the process definition's name.</param>
        /// <returns><see langword="true"/> if the input matches the process definition's name; otherwise, <see langword="false"/>.</returns>
        public bool IsNameMatch(string input)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(input);
            return NameRegex is null ? Name.Equals(input, StringComparison.OrdinalIgnoreCase) : NameRegex.IsMatch(input);
        }

        /// <summary>
        /// Determines whether the specified process name matches the process definition's name or process name (if the name is a fully qualified path), taking into account potential wildcard characters in the name and performing a case-insensitive comparison.
        /// </summary>
        /// <param name="processName">The process name to compare against the process definition's name.</param>
        /// <returns><see langword="true"/> if the process name matches the process definition's name; otherwise, <see langword="false"/>.</returns>
        public bool ProcessNameIsMatch(string processName)
        {
            ArgumentNullException.ThrowIfNull(processName);
            return ProcessNameRegex?.IsMatch(processName)
                ?? NameRegex?.IsMatch(processName)
                ?? ProcessName?.Equals(processName, StringComparison.OrdinalIgnoreCase)
                ?? Name.Equals(processName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// Gets the process name without the path component, if the process definition's name is a fully qualified path.
        /// </summary>
        [IgnoreDataMember]
        private string? ProcessName;

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        [DataMember]
        public readonly string? Description;

        /// <summary>
        /// Gets the regular expression for the process name, if the name contains wildcard characters.
        /// </summary>
        [IgnoreDataMember]
        private Regex? NameRegex;

        /// <summary>
        /// Gets the regular expression for the process name without the path component, if the process definition's name is a fully qualified path and contains wildcard characters.
        /// </summary>
        [IgnoreDataMember]
        private Regex? ProcessNameRegex;

        /// <summary>
        /// Gets the regular expression to determine if the process definition's name is a wildcard character only, which is not allowed for process definitions and can be used to validate input when creating process definitions from external sources.
        /// </summary>
        [IgnoreDataMember]
        private static readonly Regex WildcardOnlyRegex = new(@"^\*+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
