using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Validates that a path has an approved extension.
    /// </summary>
    public sealed class ValidateExtensionAttribute : ValidateEnumeratedArgumentsAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateExtensionAttribute"/> class.
        /// </summary>
        /// <param name="extensionNames">List of approved extension names to validate arguments against.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when one of the extensions provided in <paramref name="extensionNames"/> is not a valid extension.
        /// </exception>
        public ValidateExtensionAttribute(params string[] extensionNames)
        {
            ArgumentNullException.ThrowIfNull(extensionNames);
            foreach (string extension in extensionNames)
            {
                if (!extension.StartsWith(".") || extension.Length <= 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(extensionNames), extension, $"The provided argument '{extension}' is not a valid extension. Valid extensions must start with a period and be followed by one or more valid filename characters.");
                }
            }
            extensions = extensionNames;
        }

        /// <summary>
        /// Validates that an element in the argument has one of the extensions specified in the constructor.
        /// </summary>
        /// <param name="element">The argument value to validate.</param>
        /// <exception cref="ValidationMetadataException">
        /// Thrown when <paramref name="element"/> fails validation based on the configured rules.
        /// </exception>
        protected override void ValidateElement(object element)
        {
            if (element is null || element == AutomationNull.Value || element == NullString.Value)
            {
                throw new ArgumentNullException(null, "The argument is null. Provide a valid value for the argument, and then try running the command again.");
            }

            if (element is not string str)
            {
                throw new ArgumentException("The argument is not a string. Provide an argument that is a string and then try running the command again.");
            }

            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException("The argument is null, empty, or white space. Provide an argument that is not null, empty, or white space, and then try running the command again.");
            }

            string fileExtension = Path.GetExtension(str);
            foreach (string extension in ExtensionNames)
            {
                if (extension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            throw new ArgumentException($"The path argument '{str}' with extension '{fileExtension}' does not belong to the set of approved extensions: {string.Join(", ", ExtensionNames)}. Provide a path argument with an approved extension.");
        }

        private readonly string[] extensions;

        /// <summary>
        /// Gets the approved extension names.
        /// </summary>
        public IReadOnlyList<string> ExtensionNames => extensions;
    }
}
