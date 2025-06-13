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
        public static string GetSectionKeyValue(string filepath, string section, string key)
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
        public static void WriteSectionKeyValue(string filepath, string section, string? key, string? value)
        {
            Kernel32.WritePrivateProfileString(section, key, value, filepath);
        }

        /// <summary>
        /// Gets all key/value pairs in a section of an INI file.
        /// </summary>
        /// <param name="section">The section name</param>
        /// <param name="filepath">Path to the INI file</param>
        /// <returns>OrderedDictionary of key/value pairs in the section</returns>
        public static OrderedDictionary? GetSection(string filepath, string section)
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

            var dictionary = new OrderedDictionary();
            foreach (var entry in buffer.Slice(0, (int)res).ToString().Split('\0'))
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                var separatorIndex = entry.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

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
        /// <param name="content">INI content to write</param>
        /// <param name="filepath">Path to the INI file</param>
        public static void WriteSection(string filepath, string section, IDictionary? content)
        {
            if (content == null)
            {
                Kernel32.WritePrivateProfileSection(section, null, filepath);
                return;
            }

            var entries = new StringBuilder();
            if (content.Count > 0)
            {
                foreach (DictionaryEntry entry in content)
                {
                    if (!(entry.Key is System.String ||
                          entry.Key is System.Int32 ||
                          entry.Key is System.Double ||
                          entry.Key is System.Boolean ||
                          entry.Key is System.Char ||
                          entry.Key is System.Int16 ||
                          entry.Key is System.Int64 ||
                          entry.Key is System.Decimal ||
                          entry.Key is System.Single ||
                          entry.Key is System.UInt16 ||
                          entry.Key is System.UInt32 ||
                          entry.Key is System.UInt64 ||
                          entry.Key is System.Byte ||
                          entry.Key is System.SByte ||
                          entry.Key is System.Enum))
                    {
                        throw new ArgumentException($"Invalid key type: [{entry.Key?.GetType()?.FullName}]. Keys must be of type string, numeric, or boolean.", nameof(content));
                    }
                    string key = entry.Key?.ToString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        throw new ArgumentException($"Invalid key in content: Key cannot be null, empty, or whitespace. Original key type: [{entry.Key?.GetType()?.FullName}]", nameof(content));
                    }

                    if (!(entry.Value is System.String ||
                          entry.Value is System.Int32 ||
                          entry.Value is System.Double ||
                          entry.Value == null ||
                          entry.Value is System.Boolean ||
                          entry.Value is System.Char ||
                          entry.Value is System.Int16 ||
                          entry.Value is System.Int64 ||
                          entry.Value is System.Decimal ||
                          entry.Value is System.Single ||
                          entry.Value is System.UInt16 ||
                          entry.Value is System.UInt32 ||
                          entry.Value is System.UInt64 ||
                          entry.Value is System.Byte ||
                          entry.Value is System.SByte ||
                          entry.Value is System.Enum))
                    {
                        throw new ArgumentException($"Invalid value type: [{entry.Value.GetType().FullName}] for key '{entry.Key}'. Values must be null, string, numeric, or boolean.", nameof(content));
                    }
                    string value = entry.Value?.ToString()?.Trim() ?? string.Empty;

                    entries.Append(key);
                    entries.Append('=');
                    entries.Append(value);
                    entries.Append('\0');
                }
            }
            else
            {
                entries.Append('\0');
            }
            entries.Append('\0');

            Kernel32.WritePrivateProfileSection(section, entries.ToString(), filepath);
        }
    }
}
