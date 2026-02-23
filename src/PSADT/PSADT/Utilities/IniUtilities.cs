using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using PSADT.Interop;

namespace PSADT.Utilities
{
    /// <summary>
    /// Utility methods for interacting with INI files.
    /// </summary>
    public static class IniUtilities
    {
        /// <summary>
        /// Retrieves the value associated with the specified key from a given section in an INI configuration file.
        /// </summary>
        /// <remarks>This method uses the Windows API function GetPrivateProfileString to read values from
        /// INI files. Ensure that the specified file exists and is accessible. The method does not throw an exception
        /// if the key or section is not found; instead, it returns an empty string.</remarks>
        /// <param name="filepath">The path to the INI configuration file to read from. Must be a valid file path.</param>
        /// <param name="section">The name of the section within the INI file that contains the key. Cannot be null or empty.</param>
        /// <param name="key">The name of the key whose value is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A string containing the value associated with the specified key in the specified section. Returns an empty
        /// string if the key does not exist.</returns>
        public static string GetSectionKeyValue(string filepath, string section, string key)
        {
            Span<char> buffer = stackalloc char[4096]; buffer.Clear();
            return buffer.Slice(0, (int)NativeMethods.GetPrivateProfileString(section, key, null, buffer, filepath)).ToString();
        }

        /// <summary>
        /// Writes a key-value pair to a specified section in an INI file at the given file path.
        /// </summary>
        /// <remarks>This method uses the Windows API to update INI files. Ensure that the application has
        /// the necessary permissions to modify the file. Deleting a section or key is performed by passing null for the
        /// corresponding parameter.</remarks>
        /// <param name="filepath">The path to the INI file to be modified. The file must exist and be writable.</param>
        /// <param name="section">The name of the section within the INI file where the key-value pair will be written. If the section does
        /// not exist, it will be created.</param>
        /// <param name="key">The name of the key to write within the specified section. If null, the entire section is deleted.</param>
        /// <param name="value">The value to assign to the specified key. If null, the key is removed from the section.</param>
        public static void WriteSectionKeyValue(string filepath, string section, string? key, string? value)
        {
            _ = NativeMethods.WritePrivateProfileString(section, key, value, filepath);
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
                res = NativeMethods.GetPrivateProfileSection(section, buffer, filepath);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to get section [{section}] from the INI file.", ex);
            }

            OrderedDictionary dictionary = [];
            foreach (string entry in buffer.Slice(0, (int)res).ToString().Split(['\0'], StringSplitOptions.RemoveEmptyEntries))
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
            return new(buffer.Slice(0, (int)NativeMethods.GetPrivateProfileSectionNames(buffer, filepath)).ToString().Split(['\0'], StringSplitOptions.RemoveEmptyEntries));
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
                _ = NativeMethods.WritePrivateProfileSection(section, null, filepath);
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
            _ = NativeMethods.WritePrivateProfileSection(section, entries.ToString(), filepath);
        }
    }
}
