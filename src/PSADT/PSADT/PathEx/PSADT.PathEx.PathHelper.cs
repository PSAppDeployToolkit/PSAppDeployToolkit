using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using PSADT.PInvoke;
using PSADT.Logging;

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

            UnifiedLogger.Create().Message($"File name [{fileName}] resolved to fully-qualified path [{result ?? ""}].").Severity(LogLevel.Debug);

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

            using var appPathsKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(appPathsSubKey);
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
        /// Gets the directory path of the executing assembly.
        /// </summary>
        /// <returns>
        /// The directory path of the executing assembly, or the BaseDirectory property of <see cref="AppDomain.CurrentDomain"/> if the path cannot be determined.
        /// </returns>
        /// <remarks>
        /// This method retrieves the directory path by using <see cref="GetExecutingAssemblyFilePath"/> to obtain the full file path
        /// and then extracting the directory name. If it cannot determine the file path, it defaults to the BaseDirectory property of <see cref="AppDomain.CurrentDomain"/>.
        /// </remarks>
        public static string GetExecutingAssemblyDirectory()
        {
            string? assemblyLocation = GetExecutingAssemblyFilePath();

            // If we managed to get a valid file path, get its directory, else return the application base directory
            string assemblyDirectory = !string.IsNullOrWhiteSpace(assemblyLocation)
                ? Path.GetDirectoryName(assemblyLocation) ?? AppDomain.CurrentDomain.BaseDirectory
                : AppDomain.CurrentDomain.BaseDirectory;

            return RemoveTrailingSlash(assemblyDirectory);
        }

        /// <summary>
        /// Gets the file name of the executing assembly.
        /// </summary>
        /// <returns>
        /// The file name of the executing assembly, or <c>null</c> if it cannot be determined.
        /// </returns>
        /// <remarks>
        /// This method retrieves the file name (including the extension) of the executing assembly by using <see cref="GetExecutingAssemblyFilePath"/> 
        /// to obtain the full file path and then extracting the file name from the path.
        /// </remarks>
        public static string? GetExecutingAssemblyFileName()
        {
            string? assemblyLocation = GetExecutingAssemblyFilePath();

            // If we managed to get a valid file path, return the file name
            return assemblyLocation != null ? Path.GetFileName(assemblyLocation) : null;
        }

        /// <summary>
        /// Gets the file name of the executing assembly minus the file extension, or returns an empty string if it cannot be determined.
        /// </summary>
        /// <returns>
        /// The file name of the executing assembly without the extension, or an empty string if it cannot be determined.
        /// </returns>
        /// <remarks>
        /// This method returns the file name (without the extension) of the executing assembly. 
        /// If the file name cannot be determined, it defaults to an empty string.
        /// </remarks>
        public static string? GetExecutingAssemblyFileNameWithoutExtension()
        {
            string? fileName = GetExecutingAssemblyFileName();
            return !string.IsNullOrWhiteSpace(fileName) ? Path.GetFileNameWithoutExtension(fileName) : null;
        }

        /// <summary>
        /// Gets the file path of the executing assembly, considering various execution scenarios.
        /// </summary>
        /// <returns>
        /// The full file path of the executing assembly, or <c>null</c> if it cannot be determined.
        /// </returns>
        /// <remarks>
        /// This method attempts to retrieve the location of the executing assembly using several strategies:
        /// <list type="number">
        /// <item><description>Tries the Location property of <see cref="Assembly.GetExecutingAssembly()"/> to get the executing assembly's file path.</description></item>
        /// <item><description>If the assembly location is empty (e.g., single-file deployment), falls back to <see cref="AppContext.BaseDirectory"/>.</description></item>
        /// <item><description>If the entry assembly's location is available, it uses that as a fallback.</description></item>
        /// <item><description>Falls back to the BaseDirectory property of <see cref="AppDomain.CurrentDomain"/> if all else fails.</description></item>
        /// </list>
        /// </remarks>
        public static string? GetExecutingAssemblyFilePath()
        {
            // 1. Try to get the assembly location using the most direct approach: GetExecutingAssembly().Location
            var executingAssembly = Assembly.GetExecutingAssembly();
            string location = executingAssembly.Location;

            // 2. Handle cases where the location is empty (e.g., single-file executables, dynamically generated assemblies)
            if (string.IsNullOrWhiteSpace(location))
            {
                // Fallback to AppContext.BaseDirectory for single-file executables and other scenarios
                string baseDirectory = AppContext.BaseDirectory;
                if (!string.IsNullOrWhiteSpace(baseDirectory))
                {
                    var entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly != null && !string.IsNullOrWhiteSpace(entryAssembly.Location))
                    {
                        // Return full path of the entry assembly
                        location = entryAssembly.Location;
                    }
                    else
                    {
                        location = baseDirectory;
                    }
                }
            }

            // 3. If the location is still empty, fallback to the base directory of the current application domain
            if (string.IsNullOrWhiteSpace(location))
            {
                location = AppDomain.CurrentDomain.BaseDirectory;
            }

            // Return null if we still can't get a valid path
            return !string.IsNullOrWhiteSpace(location) ? location : null;
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
