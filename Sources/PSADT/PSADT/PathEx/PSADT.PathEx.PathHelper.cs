using System;
using System.IO;
using System.Linq;
using System.Text;
using PSADT.PInvoke;
using PSADT.ConsoleEx;
using Microsoft.Win32;
using System.Collections.Generic;

namespace PSADT.PathEx
{
    public static class PathHelper
    {
        private static readonly Lazy<Dictionary<string, string>> _appPaths = new Lazy<Dictionary<string, string>>(LoadAppPathsExecutables);
        private static readonly Lazy<string[]> _executableExtensions = new Lazy<string[]>(LoadExecutableExtensions);

        /// <summary>
        /// Attempts to resolve the full path for a fileName by checking extensions and search directories.
        /// </summary>
        /// <param name="fileName">The fileName to search for.</param>
        /// <param name="searchDirectories">Optional search directories to look for the fileName.</param>
        /// <returns>The full path to the fileName if found; otherwise, null.</returns>
        public static string? ResolveExecutableFullPath(string fileName, IEnumerable<string>? searchDirectories = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(fileName));
            }

            string? result = Path.HasExtension(fileName)
                ? ResolveFullPathForFileName(fileName, searchDirectories)
                : ResolveFullPathByProbingExtensions(fileName, searchDirectories);

            ConsoleHelper.DebugWrite($"File name [{fileName}] resolved to fully-qualified path [{result ?? ""}].", MessageType.Debug);

            return result;
        }

        /// <summary>
        /// Loads the executable extensions from the system's environment variables.
        /// </summary>
        /// <returns>An array of executable extensions.</returns>
        private static string[] LoadExecutableExtensions()
        {
            return Environment.GetEnvironmentVariable("PATHEXT")?.ToLowerInvariant().Split(';') ?? Array.Empty<string>();
        }

        /// <summary>
        /// Loads the executables defined in the system's App Paths registry.
        /// </summary>
        /// <returns>A dictionary mapping executable names to their full paths.</returns>
        private static Dictionary<string, string> LoadAppPathsExecutables()
        {
            var appPathsExecutables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            const string appPathsSubKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

            using var appPathsKey = Registry.LocalMachine.OpenSubKey(appPathsSubKey);
            if (appPathsKey != null)
            {
                foreach (var exeName in appPathsKey.GetSubKeyNames())
                {
                    using var exeKey = appPathsKey.OpenSubKey(exeName);
                    var fullyQualifiedFilePath = exeKey?.GetValue(string.Empty) as string;

                    if (!String.IsNullOrWhiteSpace(fullyQualifiedFilePath))
                    {
                        fullyQualifiedFilePath = Unquote(fullyQualifiedFilePath!);
                        if (Path.IsPathRooted(fullyQualifiedFilePath))
                        {
                            appPathsExecutables.Add(exeName, fullyQualifiedFilePath);
                        }
                    }
                }
            }

            return appPathsExecutables;
        }

        /// <summary>
        /// Attempts to resolve the full path for a file name by probing its extensions.
        /// </summary>
        /// <param name="fileName">The file name to search for.</param>
        /// <param name="searchDirectories">Optional search directories to look for the file name.</param>
        /// <returns>The full path to the file name if found; otherwise, null.</returns>
        private static string? ResolveFullPathByProbingExtensions(string fileName, IEnumerable<string>? searchDirectories = null)
        {
            foreach (var extension in _executableExtensions.Value)
            {
                var result = ResolveFullPathForFileName(fileName + extension, searchDirectories);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to resolve the full path for a file name by searching through the directories.
        /// </summary>
        /// <param name="fileName">The file name to search for.</param>
        /// <param name="searchDirectories">Optional search directories to look for the file.</param>
        /// <returns>The full path to the file if found; otherwise, null.</returns>
        private static string? ResolveFullPathForFileName(string fileName, IEnumerable<string>? searchDirectories = null)
        {
            return ResolveFullPathFromPathEnvironmentVariable(fileName, searchDirectories) ?? ResolveFullPathFromAppPaths(fileName);
        }

        /// <summary>
        /// Attempts to resolve the full path for a file name from the system's App Paths registry.
        /// </summary>
        /// <param name="fileName">The file name to search for in the App Paths registry.</param>
        /// <returns>The full path to the file if found; otherwise, null.</returns>
        private static string? ResolveFullPathFromAppPaths(string fileName)
        {
            return _appPaths.Value.TryGetValue(fileName, out var path) ? path : null;
        }

        /// <summary>
        /// Attempts to resolve the full path for a file name from the system's PATH environment variable.
        /// </summary>
        /// <param name="fileName">The file name to search for.</param>
        /// <param name="searchDirectories">Optional search directories to look for the file.</param>
        /// <returns>The full path to the file if found; otherwise, null.</returns>
        /// <exception cref="ArgumentException">Thrown when the file name exceeds the maximum path length.</exception>
        private static string? ResolveFullPathFromPathEnvironmentVariable(string fileName, IEnumerable<string>? searchDirectories = null)
        {
            if (fileName.Length >= NativeMethods.MAX_PATH)
            {
                throw new ArgumentException($"The executable name [{fileName}] must have fewer than [{NativeMethods.MAX_PATH}] characters.", nameof(fileName));
            }

            if (!Path.IsPathRooted(fileName))
            {
                // Use a custom method to check for the file in the PATH environment variable or search directories
                FileExistsOnPath(fileName, searchDirectories, out var resolvedFilePath);
                return resolvedFilePath;
            }

            return fileName;
        }

        /// <summary>
        /// Gets the name of the executing assembly file.
        /// </summary>
        /// <returns>The name of the executing assembly file.</returns>
        public static string GetExecutingAssemblyFileName()
        {
            // Use AppContext.BaseDirectory to get the directory containing the executable
            var location = AppContext.BaseDirectory;
            return new FileInfo(location).Name;
        }

        /// <summary>
        /// Gets the directory path of the executing assembly.
        /// </summary>
        /// <returns>The directory path of the executing assembly.</returns>
        public static string GetExecutingAssemblyDirectoryPath()
        {
            // Use AppContext.BaseDirectory to get the directory containing the executable
            var executingAssemblyDirectoryPath = AppContext.BaseDirectory;
            return RemoveTrailingSlash(executingAssemblyDirectoryPath);
        }

        /// <summary>
        /// Removes quotes from the beginning and end of a string.
        /// </summary>
        /// <param name="input">The input string to unquote.</param>
        /// <returns>The unquoted string.</returns>
        public static string Unquote(string input)
        {
            if (input.StartsWith("\"") && input.EndsWith("\""))
            {
                return input.Substring(1, input.Length - 2);
            }

            return input;
        }

        /// <summary>
        /// Removes the trailing backslash from a path string.
        /// </summary>
        /// <param name="path">The path string to process.</param>
        /// <returns>The path string without the trailing backslash.</returns>
        public static string RemoveTrailingSlash(string path)
        {
            return path.TrimEnd('\\');
        }

        /// <summary>
        /// Replaces the first occurrence of a specified string within another string,
        /// performing the search using the specified string comparison option.
        /// </summary>
        /// <param name="input">The input string in which to perform the replacement.</param>
        /// <param name="oldValue">The string to be replaced.</param>
        /// <param name="newValue">The string that replaces the <paramref name="oldValue"/>.</param>
        /// <param name="comparisonType">The string comparison option to use for the search. Default is 'OrdinalIgnoreCase'.</param>
        /// <returns>
        /// A new string that is equivalent to the input string but with the first occurrence
        /// of the <paramref name="oldValue"/> replaced by the <paramref name="newValue"/>.
        /// If <paramref name="oldValue"/> is not found, the original <paramref name="input"/> string is returned.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/>, <paramref name="oldValue"/>, or <paramref name="newValue"/> is <c>null</c>.</exception>
        public static string Replace(string input, string oldValue, string newValue, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (oldValue == null) throw new ArgumentNullException(nameof(oldValue));
            if (newValue == null) throw new ArgumentNullException(nameof(newValue));

            int index = input.IndexOf(oldValue, comparisonType);
            if (index >= 0)
            {
                input = input.Remove(index, oldValue.Length).Insert(index, newValue);
            }
            return input;
        }

        /// <summary>
        /// Checks if a file exists on the given search path directories using the PathFindOnPath API.
        /// </summary>
        /// <param name="fileName">The name of the file to search for.</param>
        /// <param name="searchDirectories">A collection of directories to search in.</param>
        /// <param name="foundFilePath">The full path to the found file, if any.</param>
        /// <returns>True if the file is found on the path; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when fileName is null or empty.</exception>
        public static bool FileExistsOnPath(string fileName, IEnumerable<string>? searchDirectories, out string? foundFilePath)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName), "File name cannot be null or empty.");
            }

            var sb = new StringBuilder(fileName, NativeMethods.MAX_PATH);

            // Initialize dirs as an empty array if searchDirectories is null, ensuring it's non-nullable
            string[] dirs = searchDirectories?.Where(d => d != null).ToArray() ?? Array.Empty<string>();
            Array.Resize(ref dirs, dirs.Length + 1);
            dirs[dirs.Length - 1] = null!;

            foundFilePath = NativeMethods.PathFindOnPath(sb, dirs) ? sb.ToString() : null;

            return foundFilePath != null;
        }
    }
}
