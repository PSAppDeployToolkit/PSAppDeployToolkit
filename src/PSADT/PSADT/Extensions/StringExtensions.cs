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
        internal static string TrimRemoveNull(this string str) => str.Replace("\0", string.Empty).Trim();
    }
}
