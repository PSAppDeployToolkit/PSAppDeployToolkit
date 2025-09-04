namespace PSADT.Extensions
{
    /// <summary>
    /// Extension methods for string manipulation.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Trims a string and removes null characters.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static string TrimRemoveNull(this string str) => str.Replace("\0", null).Trim();

        /// <summary>
        /// Trims trailing whitespace from the string and removes all null characters.
        /// </summary>
        /// <param name="str">The string to process.</param>
        /// <returns>A new string with trailing whitespace removed and all null characters replaced with an empty string.</returns>
        internal static string TrimEndRemoveNull(this string str) => str.Replace("\0", null).TrimEnd();
    }
}
