using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using PSADT.PInvokes;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.Registry
{
    /// <summary>
    /// Provides utility methods for interacting with the Windows Registry.
    /// </summary>
    public static class RegistryUtils
    {
        /// <summary>
        /// Normalizes a registry path into a standard format.
        /// </summary>
        /// <param name="keyPath">The registry key path to normalize.</param>
        /// <returns>The normalized registry key path.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyPath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="keyPath"/> is invalid.</exception>
        public static string NormalizeRegistryPath(string keyPath)
        {
            if (keyPath == null) throw new ArgumentNullException(nameof(keyPath));

            // Trim leading and trailing whitespace
            keyPath = keyPath.Trim();

            // Replace forward slashes with backslashes for consistency
            keyPath = keyPath.Replace('/', '\\');

            // Remove any duplicate slashes
            while (keyPath.Contains(@"\\"))
            {
                keyPath = keyPath.Replace(@"\\", @"\");
            }

            // Normalize shortened PowerShell registry provider prefix
            if (keyPath.StartsWith("Registry::", StringComparison.OrdinalIgnoreCase))
            {
                keyPath = keyPath.Substring("Registry::".Length);
            }

            // Normalize fully qualified PowerShell registry provider prefix
            if (keyPath.StartsWith(@"Microsoft.PowerShell.Core\Registry::", StringComparison.OrdinalIgnoreCase))
            {
                keyPath = keyPath.Substring(@"Microsoft.PowerShell.Core\Registry::".Length);
            }

            // Handle PowerShell drive-qualified paths (e.g., HKLM:Software\Microsoft)
            if (keyPath.Contains(":"))
            {
                var driveQualifiedMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "HKLM:", "HKEY_LOCAL_MACHINE"  },
                    { "HKCU:", "HKEY_CURRENT_USER"   },
                    { "HKCR:", "HKEY_CLASSES_ROOT"   },
                    { "HKU:", "HKEY_USERS"           },
                    { "HKCC:", "HKEY_CURRENT_CONFIG" }
                };

                foreach (var kvp in driveQualifiedMappings)
                {
                    if (keyPath.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        keyPath = keyPath.Replace(kvp.Key, $@"{kvp.Value}\");
                        break;
                    }
                }
            }

            // Split the key path into parts
            string[] pathParts = keyPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (pathParts.Length == 0)
            {
                throw new ArgumentException("Invalid registry path.", nameof(keyPath));
            }

            // Normalize the hive name
            var hiveMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HKCR", "HKEY_CLASSES_ROOT" },
                { "HKEY_CLASSES_ROOT", "HKEY_CLASSES_ROOT" },
                { "HKCU", "HKEY_CURRENT_USER" },
                { "HKEY_CURRENT_USER", "HKEY_CURRENT_USER" },
                { "HKLM", "HKEY_LOCAL_MACHINE" },
                { "HKEY_LOCAL_MACHINE", "HKEY_LOCAL_MACHINE" },
                { "HKU", "HKEY_USERS" },
                { "HKEY_USERS", "HKEY_USERS" },
                { "HKCC", "HKEY_CURRENT_CONFIG" },
                { "HKEY_CURRENT_CONFIG", "HKEY_CURRENT_CONFIG" }
            };

            string hiveName = pathParts[0];
            string normalizedHiveName = string.Empty;
            if (!hiveMappings.TryGetValue(hiveName, out normalizedHiveName!))
            {
                throw new ArgumentException($"Unrecognized registry hive: {hiveName}", nameof(keyPath));
            }

            // Reconstruct the normalized path
            string normalizedPath = normalizedHiveName;
            if (pathParts.Length > 1)
            {
                string subKeyPath = string.Join(@"\", pathParts, 1, pathParts.Length - 1);
                normalizedPath = @$"{normalizedHiveName}\{subKeyPath}";
            }

            return normalizedPath;
        }

        /// <summary>
        /// Normalizes a PowerShell registry path, resolving any wildcards and returning the fully qualified registry paths.
        /// </summary>
        /// <param name="registryPath">The PowerShell drive-qualified registry path that may contain wildcards.</param>
        /// <returns>
        /// A list of fully resolved registry paths. If the registry path contains wildcards, all matching paths will be included.
        /// </returns>
        /// <remarks>
        /// This method creates a PowerShell runspace to resolve registry paths using PowerShell's session state.
        /// It can handle both standard and wildcard registry paths (e.g., "HKLM:Software\\Micro*") and returns fully qualified paths.
        /// </remarks>
        /// <exception cref="Exception">
        /// An exception may occur if the registry path cannot be resolved, such as when the path is invalid or does not exist.
        /// </exception>
        public static List<string> ResolvePSRegistryPathWithWildcards(string registryPath)
        {
            // Initialize the list to hold resolved paths
            List<string> resolvedPaths = new List<string>();

            // Create a PowerShell runspace
            using (PowerShell ps = PowerShell.Create())
            {
                using Runspace runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                ps.Runspace = runspace;

                // Use the session state to resolve the registry path
                var sessionState = runspace.SessionStateProxy;
                try
                {
                    // Resolve the path with wildcards
                    Collection<string> paths = sessionState.Path.GetResolvedProviderPathFromPSPath(registryPath, out ProviderInfo providerInfo);

                    // Add resolved paths to the list
                    foreach (var path in paths)
                    {
                        resolvedPaths.Add(path);
                    }
                }
                catch
                {
                    throw;
                }
            }

            return resolvedPaths;
        }

        /// <summary>
        /// Loads a registry hive file.
        /// </summary>
        /// <param name="hiveFilePath">The path to the registry hive file.</param>
        /// <param name="hiveKey">The registry key where the hive will be loaded.</param>
        /// <param name="subKey">The subkey under the hive key where the hive will be loaded.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hiveKey"/> or <paramref name="hiveFilePath"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the operation fails, including the error code in the message.</exception>
        public static void RegLoadHiveFile(string hiveFilePath, HKEY hiveKey, string subKey)
        {
            if (subKey == null)
            {
                throw new ArgumentNullException(nameof(subKey));
            }

            if (hiveFilePath == null)
            {
                throw new ArgumentNullException(nameof(hiveFilePath));
            }

            if (!NativeMethods.RegLoadKey(hiveKey, subKey, hiveFilePath))
            {
                ErrorHandler.ThrowSystemError($@"Failed to mount registry hive file [{hiveFilePath}] at subkey [{hiveKey}\{subKey}].", SystemErrorType.Win32);
            }
        }

        /// <summary>
        /// Unloads a registry hive from the registry.
        /// </summary>
        /// <param name="hiveKey">The registry key where the hive was loaded.</param>
        /// <param name="subKey">The subkey under the hive key where the hive was loaded.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hiveKey"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the operation fails, including the error code in the message.</exception>
        public static void RegUnloadHiveFile(HKEY hiveKey, string subKey)
        {
            if (subKey == null)
            {
                throw new ArgumentNullException(nameof(subKey));
            }

            if (!NativeMethods.RegUnLoadKey(hiveKey, subKey))
            {
                ErrorHandler.ThrowSystemError($@"Failed to unload the registry hive at subkey [{hiveKey}\{subKey}].", SystemErrorType.Win32);
            }
        }

        /// <summary>
        /// Establishes a connection to a predefined registry key on another computer.
        /// </summary>
        /// <param name="machineName">The name of the remote computer. If <c>null</c>, the local computer is used.</param>
        /// <param name="hiveKey">A handle to an open registry key. This can be one of the predefined keys such as <see cref="HKEY.HKEY_LOCAL_MACHINE"/>.</param>
        /// <param name="connectedHandle">Output parameter for the handle to the connected registry key on the remote machine.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        /// <remarks>
        /// <para>
        /// The <c>RegConnectRegistry</c> function connects to a registry key on another computer. If the connection is successful, 
        /// it returns a handle to the requested registry key on the remote computer. This handle should be closed using <c>RegCloseKey</c> 
        /// when no longer needed.
        /// </para>
        /// <para>
        /// The calling process must have appropriate access rights to the remote computer. If the user does not have the necessary
        /// permissions, the function will fail.
        /// </para>
        /// <para>
        /// Ensure that the Remote Registry service is running on the remote computer. By default, this service is configured to start manually.
        /// </para>
        /// </remarks>
        internal static bool RegConnectRegistry(string? machineName, HKEY hiveKey, out SafeRegistryHandle connectedHandle)
        {
            if (!NativeMethods.RegConnectRegistry(machineName!, hiveKey, out connectedHandle))
            {
                if (machineName == null)
                {
                    ErrorHandler.ThrowSystemError($@"Failed to connect to the registry hive [{hiveKey}] on the local machine.", SystemErrorType.Win32);
                }
                else
                {
                    ErrorHandler.ThrowSystemError($@"Failed to connect to the registry hive [{hiveKey}] on remote machine [{machineName}].", SystemErrorType.Win32);
                }
            }

            return true;
        }

        /// <summary>
        /// Converts a value to the specified type, handling binary values with specified options.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="binaryValueOptions">Options for handling binary values.</param>
        /// <param name="binaryValueEncoding">The encoding for binary values.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="InvalidCastException">Thrown when conversion is not possible.</exception>
        public static T ConvertToType<T>(object value, RegistryBinaryValueOptions binaryValueOptions = RegistryBinaryValueOptions.None, RegistryBinaryValueEncoding binaryValueEncoding = RegistryBinaryValueEncoding.UTF16)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // If the value is already of the desired type, return it directly
            if (value is T t)
            {
                return t;
            }

            // Handle byte array (binary data) with the specified binary options and encoding
            if (value is byte[] byteArray)
            {
                return HandleBinaryDataConversion<T>(byteArray, binaryValueOptions, binaryValueEncoding);
            }

            throw new InvalidCastException($"Cannot convert value of type '{value.GetType()}' to type '{typeof(T)}'.");
        }

        /// <summary>
        /// Handles binary data conversion based on options and encoding.
        /// </summary>
        /// <typeparam name="T">The type to convert the binary data to.</typeparam>
        /// <param name="byteArray">The binary data.</param>
        /// <param name="binaryValueOptions">Options for handling binary values.</param>
        /// <param name="binaryValueEncoding">The encoding for binary values.</param>
        /// <returns>The converted binary data.</returns>
        public static T HandleBinaryDataConversion<T>(byte[] byteArray, RegistryBinaryValueOptions binaryValueOptions, RegistryBinaryValueEncoding binaryValueEncoding)
        {
            return binaryValueOptions switch
            {
                // Return the raw byte array if no special options are specified
                RegistryBinaryValueOptions.None => (T)Convert.ChangeType(byteArray, typeof(T)),

                // Decode the byte array as a string based on the specified encoding
                RegistryBinaryValueOptions.DecodeAsString => (T)Convert.ChangeType(GetStringFromByteArray(byteArray, binaryValueEncoding), typeof(T)),

                // Convert the byte array to a hex string
                RegistryBinaryValueOptions.ConvertToHexString => (T)Convert.ChangeType(BitConverter.ToString(byteArray).Replace("-", string.Empty), typeof(T)),

                // Fallback to returning the raw byte array if other options are not met
                _ => (T)Convert.ChangeType(byteArray, typeof(T)),
            };
        }

        /// <summary>
        /// Gets the string representation of a byte array based on the specified encoding.
        /// </summary>
        /// <param name="byteArray">The byte array to decode.</param>
        /// <param name="binaryValueEncoding">The encoding for the byte array.</param>
        /// <returns>The decoded string.</returns>
        public static string GetStringFromByteArray(byte[] byteArray, RegistryBinaryValueEncoding binaryValueEncoding)
        {
            Encoding encoding = GetEncodingObject(binaryValueEncoding);
            return encoding.GetString(byteArray);
        }

        /// <summary>
        /// Converts a string value into a byte array based on the specified binary value encoding.
        /// </summary>
        /// <param name="value">The string value to convert.</param>
        /// <param name="binaryValueEncoding">The encoding to use for the conversion.</param>
        /// <returns>A byte array representing the encoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
        public static byte[] ConvertStringToByteArray(string value, RegistryBinaryValueEncoding binaryValueEncoding = RegistryBinaryValueEncoding.UTF16)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Encoding encoding = GetEncodingObject(binaryValueEncoding);
            return encoding.GetBytes(value);
        }


        /// <summary>
        /// Retrieves the encoding based on the specified <see cref="RegistryBinaryValueEncoding"/>.
        /// </summary>
        /// <param name="encoding">The registry binary value encoding.</param>
        /// <returns>The corresponding <see cref="Encoding"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoding is unsupported.</exception>
        public static Encoding GetEncodingObject(RegistryBinaryValueEncoding encoding)
        {
            switch (encoding)
            {
                case RegistryBinaryValueEncoding.UTF8:
                    return Encoding.UTF8;
                case RegistryBinaryValueEncoding.UTF16:
                    return Encoding.Unicode;
                case RegistryBinaryValueEncoding.UTF32:
                    return Encoding.UTF32;
                case RegistryBinaryValueEncoding.ASCII:
                    return Encoding.ASCII;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), $"Unsupported encoding: {encoding}");
            }
        }

        /// <summary>
        /// Gets the registry value kind for the specified type.
        /// </summary>
        /// <param name="value">The value to determine the registry value kind for.</param>
        /// <returns>The registry value kind.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the type is not supported.</exception>
        public static RegistryValueKind GetValueKindFromType(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type valueType = value.GetType();

            if (valueType == typeof(string))
            {
                return RegistryValueKind.String;
            }
            else if (valueType == typeof(int))
            {
                return RegistryValueKind.DWord;
            }
            else if (valueType == typeof(long))
            {
                return RegistryValueKind.QWord;
            }
            else if (valueType == typeof(byte[]))
            {
                return RegistryValueKind.Binary;
            }
            else if (valueType == typeof(string[]))
            {
                return RegistryValueKind.MultiString;
            }
            else
            {
                throw new ArgumentException($"Unsupported type '{valueType}'. Only string, int, long, byte[], and string[] are supported.", nameof(value));
            }
        }

        /// <summary>
        /// Checks if a string contains percent-encoded environment variables.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>True if the string contains percent-encoded environment variables; otherwise, false.</returns>
        internal static bool HasPercentEncodedEnvironmentVariable(string input)
        {
            int firstPercentIndex = -1;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '%')
                {
                    if (firstPercentIndex != -1)
                    {
                        // We found the second percent sign, now check the substring between them
                        string betweenPercents = input.Substring(firstPercentIndex + 1, i - firstPercentIndex - 1);

                        if (IsValidEnvironmentVariableSubstring(betweenPercents))
                        {
                            return true;
                        }

                        // Update the first percent index to the current one
                        firstPercentIndex = i;
                    }
                    else
                    {
                        // Mark the first percent sign
                        firstPercentIndex = i;
                    }
                }
            }

            // No valid pair of percent signs found
            return false;
        }

        /// <summary>
        /// Checks if a substring forms a valid environment variable.
        /// </summary>
        /// <param name="substring">The substring to check.</param>
        /// <returns>True if the substring forms a valid environment variable; otherwise, false.</returns>
        internal static bool IsValidEnvironmentVariableSubstring(string substring)
        {
            bool isQuoted = substring.Length > 1 && substring[0] == '"' && substring[substring.Length - 1] == '"';

            for (int i = 0; i < substring.Length; i++)
            {
                char c = substring[i];

                if (c == '<' || c == '>' || c == '|' || c == '&' || c == '^')
                {
                    // Check if the special character is escaped
                    if (i == 0 || substring[i - 1] != '^')
                    {
                        // If the substring is not quoted, return false
                        if (!isQuoted)
                        {
                            return false;
                        }
                    }
                }
            }

            // If all special characters are properly escaped or quoted, return true
            return true;
        }


        /// <summary>
        /// Retrieves the registry hive enumeration from the hive name.
        /// </summary>
        /// <param name="hiveName">The name of the registry hive.</param>
        /// <returns>The corresponding <see cref="RegistryHive"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the hive name is invalid.</exception>
        public static RegistryHive GetRegistryHiveFromName(string hiveName)
        {
            switch (hiveName.ToUpperInvariant())
            {
                case "HKEY_CLASSES_ROOT":
                    return RegistryHive.ClassesRoot;
                case "HKEY_CURRENT_USER":
                    return RegistryHive.CurrentUser;
                case "HKEY_LOCAL_MACHINE":
                    return RegistryHive.LocalMachine;
                case "HKEY_USERS":
                    return RegistryHive.Users;
                case "HKEY_CURRENT_CONFIG":
                    return RegistryHive.CurrentConfig;
                default:
                    throw new ArgumentException($"Invalid registry hive name: {hiveName}", nameof(hiveName));
            }
        }
    }
}
