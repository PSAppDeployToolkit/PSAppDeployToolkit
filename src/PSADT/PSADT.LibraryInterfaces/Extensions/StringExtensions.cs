namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Extension methods for string manipulation.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Removes all null characters from the specified string and trims leading and trailing whitespace.
        /// </summary>
        /// <param name="str">The string from which to remove null characters. Can be null or empty.</param>
        /// <returns>A new string with all null characters removed and whitespace trimmed. Returns an empty string if the input
        /// is null or consists only of null characters and whitespace.</returns>
        internal static string RemoveNull(this string str)
        {
            return str.Replace("\0", null).Trim();
        }

        /// <summary>
        /// Trims a string and removes null characters.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static string TrimRemoveNull(this string str)
        {
            return str.RemoveNull().Trim();
        }

        /// <summary>
        /// Trims trailing whitespace from the string and removes all null characters.
        /// </summary>
        /// <param name="str">The string to process.</param>
        /// <returns>A new string with trailing whitespace removed and all null characters replaced with an empty string.</returns>
        internal static string TrimEndRemoveNull(this string str)
        {
            return str.RemoveNull().TrimEnd();
        }

        /// <summary>
        /// Removes leading and trailing whitespace and any trailing newline characters from the specified string.
        /// </summary>
        /// <param name="str">The string to trim. Can be null or empty.</param>
        /// <returns>A new string with leading and trailing whitespace and any trailing newline characters removed. If the input
        /// string is null or empty, returns the original value.</returns>
        internal static string TrimAndTrimNull(this string str)
        {
            return str.Trim().TrimEnd('\0').Trim();
        }
    }
}
