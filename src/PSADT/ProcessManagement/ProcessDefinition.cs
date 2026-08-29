using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents basic information about a process.
    /// </summary>
    [DataContract]
    public sealed record class ProcessDefinition
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

            // Compile the matchers the name implies. Assigned here rather than through a helper so that the
            // compiler can see it happen; deserialization does the same thing again once the name is restored.
            Calculated = new CalculatedFields(name);
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
        /// Sets all calculated fields after deserialization.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Calculated = new CalculatedFields(Name);
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
            return Calculated.NameRegex is null ? Name.Equals(input, StringComparison.OrdinalIgnoreCase) : Calculated.NameRegex.IsMatch(input);
        }

        /// <summary>
        /// Determines whether the specified process name matches the process definition's name or process name (if the name is a fully qualified path), taking into account potential wildcard characters in the name and performing a case-insensitive comparison.
        /// </summary>
        /// <param name="processName">The process name to compare against the process definition's name.</param>
        /// <returns><see langword="true"/> if the process name matches the process definition's name; otherwise, <see langword="false"/>.</returns>
        public bool ProcessNameIsMatch(string processName)
        {
            ArgumentNullException.ThrowIfNull(processName);
            return Calculated.ProcessNameRegex?.IsMatch(processName)
                ?? Calculated.NameRegex?.IsMatch(processName)
                ?? Calculated.ProcessName?.Equals(processName, StringComparison.OrdinalIgnoreCase)
                ?? Name.Equals(processName, StringComparison.OrdinalIgnoreCase);
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
        /// The matchers worked out from <see cref="Name"/>.
        /// </summary>
        [IgnoreDataMember]
        private CalculatedFields Calculated;

        /// <summary>
        /// Gets the regular expression to determine if the process definition's name is a wildcard character only, which is not allowed for process definitions and can be used to validate input when creating process definitions from external sources.
        /// </summary>
        [IgnoreDataMember]
        private static readonly Regex WildcardOnlyRegex = new(@"^\*+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// The matchers a definition's name is compiled into, held together so that they stay out of the definition's
        /// comparison.
        /// </summary>
        /// <remarks>A <see cref="Regex"/> compares by reference, so a definition holding one directly never equalled
        /// another built from the same name - two definitions for <c language="text">note*</c> came out unequal while the generated
        /// <c language="csharp">ToString</c> rendered them identically. Everything here is worked out from the name alone, which the
        /// definition already compares, so the right answer is for none of it to count towards the comparison: hence
        /// comparing by the name it was built from, which always agrees with the definition's own comparison of that
        /// name. <para> They are still compiled once and kept, as they were: the alternative of building them on each
        /// call would compile a pattern for every process on the machine, on every poll. </para></remarks>
        /// <param name="name">The name to compile.</param>
        private sealed class CalculatedFields(string name) : IEquatable<CalculatedFields>
        {
            /// <summary>
            /// Gets the process name without the path component, if the name is a fully qualified path.
            /// </summary>
            internal string? ProcessName { get; } = Path.IsPathFullyQualified(name) ? Path.GetFileNameWithoutExtension(name) : null;

            /// <summary>
            /// Gets the regular expression for the name, if it contains wildcard characters.
            /// </summary>
            internal Regex? NameRegex { get; } = name.Contains('*', StringComparison.Ordinal)
                ? new($"^{Regex.Escape(name).Replace("\\*", ".*", StringComparison.Ordinal)}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
                : null;

            /// <summary>
            /// Gets the regular expression for the process name without the path component, if the name is a fully
            /// qualified path and contains wildcard characters.
            /// </summary>
            internal Regex? ProcessNameRegex { get; } = name.Contains('*', StringComparison.Ordinal) && Path.IsPathFullyQualified(name)
                ? new($"^{Regex.Escape(Path.GetFileNameWithoutExtension(name)).Replace("\\*", ".*", StringComparison.Ordinal)}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
                : null;

            /// <summary>
            /// Determines whether these were compiled from the same name as another.
            /// </summary>
            /// <param name="other">The matchers to compare against.</param>
            /// <returns><see langword="true"/> if both were compiled from the same name; otherwise, <see langword="false"/>.</returns>
            public bool Equals([NotNullWhen(true)] CalculatedFields? other)
            {
                return ReferenceEquals(this, other) || (other is not null && _name.Equals(other._name, StringComparison.Ordinal));
            }

            /// <inheritdoc/>
            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return Equals(obj as CalculatedFields);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return StringComparer.Ordinal.GetHashCode(_name);
            }

            /// <summary>
            /// The name these were compiled from.
            /// </summary>
            private readonly string _name = name;
        }
    }
}
