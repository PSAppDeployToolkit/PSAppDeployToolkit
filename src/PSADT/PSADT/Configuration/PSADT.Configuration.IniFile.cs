using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PSADT.PInvokes;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.Configuration
{
    public static class IniFile
    {
        /// <summary>
        /// Retrieves a value from an INI file for the specified section and key.
        /// </summary>
        /// <param name="section">The section in the INI file.</param>
        /// <param name="key">The key within the section whose value is to be retrieved.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <returns>The value associated with the specified section and key in the INI file, or an empty string if not found.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if both section and key are null or if the method fails to retrieve the value.</exception>
        public static string GetSectionKeyValue(string section, string key, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            if (!NativeMethods.GetIniPrivateProfileString(section, key, null, filepath, out string result))
            {
                ErrorHandler.ThrowSystemError($"Failed to retrieve value for section '{section}' and key '{key}' from file '{filepath}'.", SystemErrorType.Win32);
            }

            return result;
        }

        /// <summary>
        /// Writes a value to the specified section and key in an INI file.
        /// </summary>
        /// <param name="section">The section in the INI file where the value will be written.</param>
        /// <param name="key">The key within the section where the value will be written.</param>
        /// <param name="value">The value to write for the specified key in the section.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if both section and key are null.</exception>
        public static void WriteSectionKeyValue(string section, string key, string value, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            if (!NativeMethods.WriteIniPrivateProfileString(section, key, value, filepath))
            {
                ErrorHandler.ThrowSystemError($"Failed to write value '{value}' for section '{section}' and key '{key}' to file '{filepath}'.", SystemErrorType.Win32);
            }
        }

        /// <summary>
        /// Retrieves all keys from the specified section in the INI file.
        /// </summary>
        /// <param name="section">The section in the INI file.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <returns>A list of all keys in the section, or an empty list if the section does not exist or contains no keys.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if retrieving the keys fails.</exception>
        public static List<string> GetAllSectionKeys(string section, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            if (!NativeMethods.GetIniPrivateProfileString(section, null, null, filepath, out string result))
            {
                ErrorHandler.ThrowSystemError($"Failed to retrieve keys for section '{section}' in file '{filepath}'.", SystemErrorType.Win32);
            }

            // Split the result by null terminator and return the list of keys
            return result.Split('\0').Where(key => !string.IsNullOrWhiteSpace(key)).ToList();
        }

        /// <summary>
        /// Retrieves all sections from the INI file.
        /// </summary>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <returns>A list of all sections in the INI file, or an empty list if no sections are found.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if retrieving the sections fails.</exception>
        public static List<string> GetAllSections(string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            if (!NativeMethods.GetIniPrivateProfileString(null, null, null, filepath, out string result))
            {
                ErrorHandler.ThrowSystemError($"Failed to retrieve sections from file '{filepath}'.", SystemErrorType.Win32);
            }

            // Split the result by null terminator and return the list of sections
            return result.Split('\0').Where(section => !string.IsNullOrWhiteSpace(section)).ToList();
        }

        /// <summary>
        /// Checks if a section exists in the INI file.
        /// </summary>
        /// <param name="section">The section to check.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <returns><c>true</c> if the section exists; otherwise, <c>false</c>.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        public static bool SectionExists(string section, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            var sections = GetAllSections(filepath);
            return sections.Contains(section);
        }

        /// <summary>
        /// Checks if a key exists in a section of the INI file.
        /// </summary>
        /// <param name="section">The section to check.</param>
        /// <param name="key">The key to check for.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <returns><c>true</c> if the key exists in the specified section; otherwise, <c>false</c>.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        public static bool KeyExists(string section, string key, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            var keys = GetAllSectionKeys(section, filepath);
            return keys.Contains(key);
        }

        /// <summary>
        /// Deletes an entire section from the INI file.
        /// </summary>
        /// <param name="section">The section to delete.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        public static void DeleteSection(string section, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            if (!NativeMethods.WriteIniPrivateProfileString(section, null, null, filepath))
            {
                ErrorHandler.ThrowSystemError($"Failed to delete section '{section}' from file '{filepath}'.", SystemErrorType.Win32);
            }
        }

        /// <summary>
        /// Deletes a key in a specified section from an INI file.
        /// </summary>
        /// <param name="section">The section in the INI file containing the key.</param>
        /// <param name="key">The key to delete. This cannot be <c>null</c>.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <returns><c>true</c> if the key is successfully deleted; otherwise, <c>false</c>.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="key"/> is <c>null</c>.</exception>
        public static bool DeleteSectionKey(string section, string key, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), "The key to delete cannot be null.");
            }

            // Calling the native method with a null value for lpString deletes the specified key
            return NativeMethods.WriteIniPrivateProfileString(section, key, null, filepath);
        }

        /// <summary>
        /// Clears all keys in the specified section of the INI file, leaving the section intact.
        /// </summary>
        /// <param name="section">The section in the INI file to clear.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <returns><c>true</c> if the operation is successful; otherwise, <c>false</c>.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the native method fails.</exception>
        public static void ClearSectionKeys(string section, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            // Retrieve all the keys in the section
            var keys = GetAllSectionKeys(section, filepath);

            // Delete each key
            foreach (var key in keys)
            {
                if (!NativeMethods.WriteIniPrivateProfileString(section, key, null, filepath))
                {
                    ErrorHandler.ThrowSystemError($"Failed to clear key '{key}' in section '{section}' from file '{filepath}'.", SystemErrorType.Win32);
                }
            }
        }

        /// <summary>
        /// Retrieves all key-value pairs from a specified section in an INI file.
        /// </summary>
        /// <param name="section">The section from which to retrieve key-value pairs.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <returns>A dictionary of key-value pairs from the section.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        public static Dictionary<string, string> GetSectionKeyValuePairs(string section, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            var keyValues = new Dictionary<string, string>();
            var keys = GetAllSectionKeys(section, filepath);

            foreach (var key in keys)
            {
                var value = GetSectionKeyValue(section, key, filepath);
                keyValues[key] = value;
            }

            return keyValues;
        }

        /// <summary>
        /// Writes multiple key-value pairs to a section in the INI file.
        /// </summary>
        /// <param name="section">The section where the key-value pairs will be written.</param>
        /// <param name="keyValues">A dictionary of key-value pairs to write.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        public static void WriteSectionKeyValuePairs(string section, Dictionary<string, string> keyValues, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            foreach (var kvp in keyValues)
            {
                if (!NativeMethods.WriteIniPrivateProfileString(section, kvp.Key, kvp.Value, filepath))
                {
                    ErrorHandler.ThrowSystemError($"Failed to write key '{kvp.Key}' with value '{kvp.Value}' to section '{section}' in file '{filepath}'.", SystemErrorType.Win32);
                }
            }
        }

        /// <summary>
        /// Copies all key-value pairs from one section to another.
        /// </summary>
        /// <param name="sourceSection">The source section to copy from.</param>
        /// <param name="targetSection">The target section to copy to.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        public static void CopySection(string sourceSection, string targetSection, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            var keyValues = GetSectionKeyValuePairs(sourceSection, filepath);
            WriteSectionKeyValuePairs(targetSection, keyValues, filepath);
        }

        /// <summary>
        /// Moves a section by copying its key-value pairs to a new section and deleting the original section.
        /// </summary>
        /// <param name="sourceSection">The source section to move.</param>
        /// <param name="targetSection">The target section to move to.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        public static void MoveSection(string sourceSection, string targetSection, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            CopySection(sourceSection, targetSection, filepath);
            DeleteSection(sourceSection, filepath);
        }

        /// <summary>
        /// Renames a key by copying its value to a new key and deleting the old key.
        /// </summary>
        /// <param name="section">The section where the key exists.</param>
        /// <param name="oldKey">The key to rename.</param>
        /// <param name="newKey">The new key name.</param>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        public static void RenameSectionKey(string section, string oldKey, string newKey, string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            var value = GetSectionKeyValue(section, oldKey, filepath);
            WriteSectionKeyValue(section, newKey, value, filepath);
            DeleteSectionKey(section, oldKey, filepath);
        }

        /// <summary>
        /// Merges the contents of one INI file into another.
        /// </summary>
        /// <param name="sourceFile">The source INI file to merge from.</param>
        /// <param name="targetFile">The target INI file to merge into.</param>
        /// <exception cref="FileNotFoundException">Thrown if either the source or target INI file does not exist.</exception>
        public static void MergeIniFiles(string sourceFile, string targetFile)
        {
            if (!File.Exists(sourceFile))
            {
                throw new FileNotFoundException($"The source INI file '{sourceFile}' does not exist.");
            }

            if (!File.Exists(targetFile))
            {
                throw new FileNotFoundException($"The target INI file '{targetFile}' does not exist.");
            }

            var sections = GetAllSections(sourceFile);
            foreach (var section in sections)
            {
                var keyValues = GetSectionKeyValuePairs(section, sourceFile);
                WriteSectionKeyValuePairs(section, keyValues, targetFile);
            }
        }

        /// <summary>
        /// Creates a backup of the INI file.
        /// </summary>
        /// <param name="filepath">The full file path to the INI file.</param>
        /// <param name="backupFilepath">The full file path to the backup file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the INI file does not exist.</exception>
        public static void BackupIniFile(string filepath, string backupFilepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"The INI file '{filepath}' does not exist.");
            }

            File.Copy(filepath, backupFilepath, overwrite: true);
        }

        /// <summary>
        /// Restores an INI file from a backup.
        /// </summary>
        /// <param name="backupFilepath">The full file path to the backup INI file.</param>
        /// <param name="targetFilepath">The full file path to the INI file to restore to.</param>
        /// <exception cref="FileNotFoundException">Thrown if the backup INI file does not exist.</exception>
        public static void RestoreIniFile(string backupFilepath, string targetFilepath)
        {
            if (!File.Exists(backupFilepath))
            {
                throw new FileNotFoundException($"The backup INI file '{backupFilepath}' does not exist.");
            }

            File.Copy(backupFilepath, targetFilepath, overwrite: true);
        }
    }

}
