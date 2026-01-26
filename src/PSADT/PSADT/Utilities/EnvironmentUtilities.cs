using System;
using System.Collections;
using Microsoft.Win32;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides utility methods for retrieving, setting, and removing environment variables for the current process,
    /// user, or machine.
    /// </summary>
    /// <remarks>This class offers static methods to manage environment variables across different scopes,
    /// including the current process, user profile, and machine-wide settings. Changes to environment variables may
    /// require appropriate permissions and, for user or machine targets, may not take effect until a new process is
    /// started. All methods are thread-safe and do not persist changes beyond the lifetime of the process unless
    /// applied to user or machine targets.</remarks>
    public static class EnvironmentUtilities
    {
        /// <summary>
        /// Retrieves the value of the specified environment variable from the current process.
        /// </summary>
        /// <remarks>If the environment variable does not exist, the method returns <see
        /// langword="null"/>. The search is case-sensitive on Linux and macOS, but case-insensitive on
        /// Windows.</remarks>
        /// <param name="variable">The name of the environment variable to retrieve. Cannot be null.</param>
        /// <returns>The value of the environment variable specified by <paramref name="variable"/> if found; otherwise, <see
        /// langword="null"/>.</returns>
        public static string? GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }

        /// <summary>
        /// Retrieves the value of an environment variable from the specified target.
        /// </summary>
        /// <remarks>If multiple targets contain environment variables with the same name, only the value
        /// from the specified target is returned. The method does not throw an exception if the variable does not
        /// exist; it returns null instead.</remarks>
        /// <param name="variable">The name of the environment variable to retrieve. Cannot be null.</param>
        /// <param name="target">An enumeration value that specifies the location from which to retrieve the environment variable, such as
        /// the current process, user, or machine.</param>
        /// <returns>The value of the environment variable specified by <paramref name="variable"/> from the given <paramref
        /// name="target"/>. Returns null if the environment variable is not found.</returns>
        public static string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            return Environment.GetEnvironmentVariable(variable, target);
        }

        /// <summary>
        /// Retrieves all environment variable names and their values from the current process.
        /// </summary>
        /// <remarks>The returned dictionary contains environment variables for the current process only.
        /// The set of variables may differ between operating systems and user contexts.</remarks>
        /// <returns>An <see cref="IDictionary"/> containing the environment variable names and their values. Each entry's key is
        /// the variable name, and the value is the variable's value as a string.</returns>
        public static IDictionary GetEnvironmentVariables()
        {
            return Environment.GetEnvironmentVariables();
        }

        /// <summary>
        /// Sets the value of an environment variable for the current process.
        /// </summary>
        /// <remarks>This method affects only the environment variables of the current process. Changes do
        /// not persist after the process ends and do not affect the system or user environment variables.</remarks>
        /// <param name="variable">The name of the environment variable to set. Cannot be null or empty.</param>
        /// <param name="value">The value to assign to the environment variable. If null, the environment variable is deleted.</param>
        public static void SetEnvironmentVariable(string variable, string? value)
        {
            Environment.SetEnvironmentVariable(variable, value);
        }

        /// <summary>
        /// Creates, modifies, or deletes an environment variable for the current process, user, or machine.
        /// </summary>
        /// <remarks>If the environment variable specified by variable does not exist and value is not
        /// null, the variable is created. If value is null, the environment variable is deleted. The effect of this
        /// method depends on the specified target: Process, User, or Machine. Changes to User or Machine variables may
        /// not be visible to other processes until they are restarted.</remarks>
        /// <param name="variable">The name of the environment variable to create, modify, or delete. Cannot be null or empty.</param>
        /// <param name="value">The value to assign to the environment variable. If null, the environment variable is deleted.</param>
        /// <param name="target">One of the enumeration values that specifies the location where the environment variable is stored.</param>
        public static void SetEnvironmentVariable(string variable, string? value, EnvironmentVariableTarget target)
        {
            Environment.SetEnvironmentVariable(variable, value, target);
        }

        /// <summary>
        /// Sets the value of an environment variable for the specified target.
        /// </summary>
        /// <remarks>If the environment variable does not exist, it will be created. If <paramref
        /// name="value"/> is null, the environment variable will be removed from the specified target. Changes to user
        /// or machine environment variables may require administrative privileges and may not take effect until a new
        /// process is started.</remarks>
        /// <param name="variable">The name of the environment variable to set. Cannot be null or empty.</param>
        /// <param name="value">The value to assign to the environment variable. If null, the variable will be deleted.</param>
        /// <param name="target">An enumeration value that specifies the location where the environment variable is set, such as the current
        /// process, user, or machine.</param>
        /// <param name="expandable">If set to <see langword="true"/>, the value will be treated as an expandable string (e.g., it can contain references to other environment variables).</param>
        public static void SetEnvironmentVariable(string variable, string? value, EnvironmentVariableTarget target, bool expandable = false)
        {
            // Use the built-in method for process-level variables.
            if (target == EnvironmentVariableTarget.Process)
            {
                SetEnvironmentVariable(variable, value);
                return;
            }

            // Validate the variable name.
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }
            if (variable.Length == 0)
            {
                throw new ArgumentException("String cannot be of zero length.", nameof(variable));
            }
            if (string.IsNullOrWhiteSpace(variable))
            {
                throw new ArgumentException("String cannot contain only whitespace characters.", nameof(variable));
            }
            if (variable[0] == '\0')
            {
                throw new ArgumentException("The first char in the string is the null character.", nameof(variable));
            }
            if (variable.Length >= 1024)
            {
                throw new ArgumentException("Environment variable name or value is too long.");
            }
            if (variable.Contains("="))
            {
                throw new ArgumentException("Environment variable name cannot contain equal character.");
            }

            // Treat empty or whitespace-only values as null (deletion).
            if (string.IsNullOrWhiteSpace(value) || value![0] == '\0')
            {
                value = null;
            }

            // Set the environment variable in the registry for user or machine targets.
            switch (target)
            {
                case EnvironmentVariableTarget.Machine:
                    {
                        using RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Session Manager\\Environment", writable: true) ?? throw new InvalidOperationException("Could not open registry key for machine environment variables.");
                        if (value is null)
                        {
                            registryKey.DeleteValue(variable, throwOnMissingValue: false);
                        }
                        else
                        {
                            registryKey.SetValue(variable, value, expandable ? RegistryValueKind.ExpandString : RegistryValueKind.String);
                        }
                        break;
                    }
                case EnvironmentVariableTarget.User:
                    {
                        if (variable.Length >= 255)
                        {
                            throw new ArgumentException("Environment variable name or value is too long.");
                        }
                        using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Environment", writable: true) ?? throw new InvalidOperationException("Could not open registry key for user environment variables.");
                        if (value == null)
                        {
                            registryKey.DeleteValue(variable, throwOnMissingValue: false);
                        }
                        else
                        {
                            registryKey.SetValue(variable, value, expandable ? RegistryValueKind.ExpandString : RegistryValueKind.String);
                        }
                        break;
                    }
                case EnvironmentVariableTarget.Process:
                    throw new InvalidOperationException("Process target should be handled separately.");
                default:
                    throw new ArgumentException($"Illegal enum value: {target}.");
            }

            // Refresh environment variables in the current process.
            ShellUtilities.RefreshEnvironmentVariables();
        }

        /// <summary>
        /// Removes the specified environment variable from the current process.
        /// </summary>
        /// <remarks>This method only affects the environment variables of the current process. It does
        /// not remove environment variables from the system or user environment. If the specified variable does not
        /// exist, no action is taken.</remarks>
        /// <param name="variable">The name of the environment variable to remove. Cannot be null.</param>
        public static void RemoveEnvironmentVariable(string variable)
        {
            Environment.SetEnvironmentVariable(variable, null);
        }

        /// <summary>
        /// Removes the specified environment variable from the given environment variable target.
        /// </summary>
        /// <remarks>If the specified environment variable does not exist, no action is taken. Removing a
        /// machine or user environment variable may require appropriate permissions.</remarks>
        /// <param name="variable">The name of the environment variable to remove. Cannot be null.</param>
        /// <param name="target">An enumeration value that specifies whether the environment variable is removed from the current process,
        /// user, or machine.</param>
        public static void RemoveEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            Environment.SetEnvironmentVariable(variable, null, target);
        }
    }
}
