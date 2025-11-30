using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PSADT.Extensions;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides utility methods for processing strings and collections of strings.
    /// </summary>
    /// <remarks>The <see cref="MiscUtilities"/> class includes methods to trim leading and trailing lines
    /// that are empty or consist only of white-space characters. These methods are useful for cleaning up text data
    /// before further processing.</remarks>
    public static class MiscUtilities
    {
        /// <summary>
        /// Trims leading and trailing lines that are null or consist only of white-space characters from the specified
        /// collection of strings.
        /// </summary>
        /// <param name="value">The collection of strings to process. Each string represents a line.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of strings with leading and trailing white-space lines removed. The order of
        /// the remaining lines is preserved.</returns>
        public static IReadOnlyList<string> TrimLeadingTrailingLines(IEnumerable<string> value) => value is not null ? new ReadOnlyCollection<string>(value.Select(static s => s.TrimEndRemoveNull()).SkipWhile(string.IsNullOrWhiteSpace).Reverse().SkipWhile(string.IsNullOrWhiteSpace).Reverse().ToArray()) : throw new ArgumentNullException("The input collection cannot be null.", (Exception?)null);

        /// <summary>
        /// Trims leading and trailing empty lines from the specified string.
        /// </summary>
        /// <param name="value">The string from which to trim leading and trailing empty lines.</param>
        /// <returns>A string with leading and trailing empty lines removed. If the input string is empty or consists only of
        /// whitespace, returns an empty string.</returns>
        public static string TrimLeadingTrailingLines(string value) => string.Join("\n", TrimLeadingTrailingLines(value.Split('\n')));
    }
}
