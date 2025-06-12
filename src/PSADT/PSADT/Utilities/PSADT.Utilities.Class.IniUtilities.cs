using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using PSADT.Extensions;
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
            Span<char> buffer = stackalloc char[4096];
            var res = Kernel32.GetPrivateProfileString(section, key, null, buffer, filepath);
            return buffer.Slice(0, (int)res).ToString().TrimRemoveNull();
        }

        /// <summary>
        /// Sets the value of a key in a section of an INI file.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="filepath"></param>
        public static void WriteSectionKeyValue(string section, string? key, string? value, string filepath)
        {
            Kernel32.WritePrivateProfileString(section, key, value, filepath);
        }

        /// <summary>
        /// Gets all key/value pairs in a section of an INI file.
        /// </summary>
        /// <param name="section">The section name</param>
        /// <param name="filepath">Path to the INI file</param>
        /// <returns>OrderedDictionary of key/value pairs in the section</returns>
        public static OrderedDictionary? GetSection(string section, string filepath)
        {
            var sections = GetSectionNames(filepath);
            if (!sections.Contains(section, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Section [{section}] was not found in the INI file. Sections found: {string.Join(", ", sections)}", nameof(section));
            }

            Span<char> buffer = stackalloc char[65536];

            uint res;
            try
            {
                res = Kernel32.GetPrivateProfileSection(section, buffer, filepath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get section [{section}] from the INI file.", ex);
            }

            var entries = buffer.Slice(0, (int)res).ToString().Split('\0');
            var dictionary = new OrderedDictionary();

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;

                var separatorIndex = entry.IndexOf('=');
                if (separatorIndex <= 0) continue;

                var key = entry.Substring(0, separatorIndex);
                var value = entry.Substring(separatorIndex + 1);

                if (dictionary.Contains(key))
                {
                    dictionary[key] = value;
                }
                else
                {
                    dictionary.Add(key, value);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Gets all section names from an INI file.
        /// </summary>
        /// <param name="filepath">Path to the INI file</param>
        /// <returns>Array of section names</returns>
        private static ReadOnlyCollection<string> GetSectionNames(string filepath)
        {
            Span<char> buffer = stackalloc char[65536];
            var res = Kernel32.GetPrivateProfileSectionNames(buffer, filepath);

            return buffer.Slice(0, (int)res).ToString().Split('\0').Where(name => !string.IsNullOrWhiteSpace(name)).ToList().AsReadOnly();
        }

        /// <summary>
        /// Writes multiple key/value pairs to a section in an INI file.
        /// </summary>
        /// <param name="section">The section name</param>
        /// <param name="keyValuePairs">Collection of key/value pairs to write</param>
        /// <param name="filepath">Path to the INI file</param>
        public static void WriteSection(string section, IDictionary? keyValuePairs, string filepath)
        {
            if (keyValuePairs == null)
            {
                Kernel32.WritePrivateProfileSection(section, null, filepath);
                return;
            }

            var entries = new StringBuilder();
            if (keyValuePairs.Count == 0)
            {
                entries.Append('\0');
            }
            else
            {
                foreach (DictionaryEntry kvp in keyValuePairs)
                {
                    string key = kvp.Key?.ToString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        throw new ArgumentException($"Invalid key in dictionary: Key cannot be null, empty, or whitespace.", nameof(keyValuePairs));
                    }

                    entries.Append(key);
                    entries.Append('=');
                    entries.Append(kvp.Value);
                    entries.Append('\0');
                }
            }
            entries.Append('\0');

            Kernel32.WritePrivateProfileSection(section, entries.ToString(), filepath);
        }
    }
}
