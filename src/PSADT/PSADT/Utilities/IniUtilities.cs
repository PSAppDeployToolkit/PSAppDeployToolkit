using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
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
            uint res = Kernel32.GetPrivateProfileString(section, key, null, buffer, filepath);
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
            _ = Kernel32.WritePrivateProfileString(section, key, value, filepath);
        }

        /// <summary>
        /// Gets all key/value pairs in a section of an INI file.
        /// </summary>
        /// <param name="section">The section name</param>
        /// <param name="filepath">Path to the INI file</param>
        /// <returns>OrderedDictionary of key/value pairs in the section</returns>
        public static OrderedDictionary? GetSection(string filepath, string section)
        {
            ReadOnlyCollection<string> sections = GetSectionNames(filepath);
            if (!sections.Contains(section, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Section [{section}] was not found in the INI file. Sections found: {string.Join(", ", sections)}", nameof(section));
            }

            Span<char> buffer = new char[65536];
            uint res;
            try
            {
                res = Kernel32.GetPrivateProfileSection(section, buffer, filepath);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to get section [{section}] from the INI file.", ex);
            }

            OrderedDictionary dictionary = [];
            foreach (string entry in buffer.Slice(0, (int)res).ToString().Split('\0'))
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                int separatorIndex = entry.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                string key = entry.Substring(0, separatorIndex);
                string value = entry.Substring(separatorIndex + 1);
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
            Span<char> buffer = new char[65536];
            uint res = Kernel32.GetPrivateProfileSectionNames(buffer, filepath);
            return new([.. buffer.Slice(0, (int)res).ToString().Split(['\0'], StringSplitOptions.RemoveEmptyEntries).Where(name => !string.IsNullOrWhiteSpace(name))]);
        }

        /// <summary>
        /// Writes multiple key/value pairs to a section in an INI file.
        /// </summary>
        /// <param name="section">The section name</param>
        /// <param name="content">INI content to write</param>
        /// <param name="filepath">Path to the INI file</param>
        public static void WriteSection(string filepath, string section, IDictionary? content)
        {
            if (content is null)
            {
                _ = Kernel32.WritePrivateProfileSection(section, null, filepath);
                return;
            }

            StringBuilder entries = new();
            if (content.Count > 0)
            {
                foreach (DictionaryEntry entry in content)
                {
                    if (entry.Key is not (string or ValueType))
                    {
                        throw new ArgumentException($"Invalid key type: [{entry.Key?.GetType()?.FullName}]. Keys must be of type string, numeric, or boolean.", nameof(content));
                    }

                    string key = entry.Key?.ToString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        throw new ArgumentException($"Invalid key in content: Key cannot be null, empty, or whitespace. Original key type: [{entry.Key?.GetType()?.FullName}]", nameof(content));
                    }

                    if (entry.Value is not (string or ValueType or null))
                    {
                        throw new ArgumentException($"Invalid value type: [{entry.Value.GetType().FullName}] for key '{entry.Key}'. Values must be null, string, numeric, or boolean.", nameof(content));
                    }
                    _ = entries.Append(key);
                    _ = entries.Append('=');
                    _ = entries.Append(entry.Value?.ToString()?.Trim() ?? string.Empty);
                    _ = entries.Append('\0');
                }
            }
            else
            {
                _ = entries.Append('\0');
            }
            _ = entries.Append('\0');
            _ = Kernel32.WritePrivateProfileSection(section, entries.ToString(), filepath);
        }
    }
}
