using PSADT.LibraryInterfaces;

namespace PSADT.Utilities
{
    /// <summary>
    /// Utility methods for interacting with INI files.
    /// </summary>
    public static class IniUtilities
    {
        /// <summary>
        /// Gets the value of a key in a section of an INI file.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string GetSectionKeyValue(string section, string key, string filepath)
        {
            var buffer = new char[4096];
            var res = Kernel32.GetPrivateProfileString(section, key, "", buffer, filepath);
            return new string(buffer, 0, (int)res);
        }

        /// <summary>
        /// Sets the value of a key in a section of an INI file.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="filepath"></param>
        public static void WriteSectionKeyValue(string section, string key, string value, string filepath)
        {
            Kernel32.WritePrivateProfileString(section, key, value, filepath);
        }
    }
}
