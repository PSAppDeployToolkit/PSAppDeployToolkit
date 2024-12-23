using System;
using System.Linq;
using System.Security;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using PSADT.PInvokes;
using PSADT.Logging;
using PSADT.Diagnostics.Validation;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.Registry
{
    /// <summary>
    /// Provides methods for interacting with registry keys across different registry views.
    /// </summary>
    public class CompositeRegistryKeyWrapper : IDisposable
    {
        private readonly List<RegistryKey> _baseHiveWithViews = new List<RegistryKey>();
        private readonly List<RegistryKeyInfo> _keyPathInfoList = new List<RegistryKeyInfo>();
        private readonly RegistryHiveLoader? _hiveLoader;
        private readonly object _syncRoot = new object();
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeRegistryKeyWrapper"/> class.
        /// </summary>
        /// <param name="hiveLoader">The loader object for a registry hive file.</param>
        public CompositeRegistryKeyWrapper(RegistryHiveLoader? hiveLoader = null)
        {
            _hiveLoader = hiveLoader;
        }

        /// <summary>
        /// Adds a base registry key hive to the wrapper.
        /// </summary>
        /// <param name="baseHiveWithView">The base registry key hive with a view.</param>
        /// <param name="keyPathInfo">Information about the registry key path.</param>
        /// <exception cref="ArgumentNullException">Thrown if the base key or keyPathInfo is null.</exception>
        public void AddBaseHive(RegistryKey baseHiveWithView, RegistryKeyInfo keyPathInfo)
        {
            GuardAgainst.ThrowIfNull(baseHiveWithView);
            GuardAgainst.ThrowIfNull(keyPathInfo);

            lock (_syncRoot)
            {
                _baseHiveWithViews.Add(baseHiveWithView);
                _keyPathInfoList.Add(keyPathInfo);
            }

            UnifiedLogger.Create()
                    .Message($"Added base key hive [{baseHiveWithView.Name}] with view [{keyPathInfo.ViewName}].")
                    .Severity(LogLevel.Verbose).Log();
        }

        /// <summary>
        /// Retrieves a value from the registry.
        /// </summary>
        /// <param name="valueName">The name of the value to retrieve. If null, retrieves the default value of the subkey.</param>
        /// <param name="valueOptions">Options for getting the value.</param>
        /// <param name="binaryValueOptions">Options for handling binary values.</param>
        /// <param name="binaryValueEncoding">The encoding for binary values.</param>
        /// <returns>The value from the registry, or default(T) if the value is not found.</returns>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions.</exception>
        /// <exception cref="AggregateException">Thrown when the operation fails and multiple exceptions have occurred.</exception>
        public object GetValue(string? valueName,
                       RegistryValueOptions valueOptions = RegistryValueOptions.DoNotExpandEnvironmentNames,
                       RegistryBinaryValueOptions binaryValueOptions = RegistryBinaryValueOptions.None,
                       RegistryBinaryValueEncoding binaryValueEncoding = RegistryBinaryValueEncoding.UTF16)
        {
            return GetValue<object>(valueName, valueOptions, binaryValueOptions, binaryValueEncoding);
        }

        /// <summary>
        /// Retrieves a value from the registry.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="valueName">The name of the value to retrieve. If null, retrieves the default value of the subkey.</param>
        /// <param name="valueOptions">Options for getting the value.</param>
        /// <param name="binaryValueOptions">Options for handling binary values.</param>
        /// <param name="binaryValueEncoding">The encoding for binary values.</param>
        /// <returns>The value from the registry, or default(T) if the value is not found.</returns>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions.</exception>
        /// <exception cref="AggregateException">Thrown when the operation fails and multiple exceptions have occurred.</exception>
        public T GetValue<T>(string? valueName,
                              [Optional, DefaultParameterValue(RegistryValueOptions.DoNotExpandEnvironmentNames)] RegistryValueOptions valueOptions,
                              [Optional, DefaultParameterValue(RegistryBinaryValueOptions.None)] RegistryBinaryValueOptions binaryValueOptions,
                              [Optional, DefaultParameterValue(RegistryBinaryValueEncoding.UTF16)] RegistryBinaryValueEncoding binaryValueEncoding)
        {
            string valueNameString = valueName ?? "(Default)";

            UnifiedLogger.Create()
                .Message($"Attempting to retrieve value [{valueName}] from registry key [{_keyPathInfoList[0].SubKey}].")
                .Severity(LogLevel.Verbose)
                .Log();

            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();

            for (int viewCount = 0; viewCount < _baseHiveWithViews.Count; viewCount++)
            {
                RegistryKey baseKey = _baseHiveWithViews[viewCount];
                RegistryKeyInfo keyPathInfo = _keyPathInfoList[viewCount];
                viewsChecked.Add(keyPathInfo.ViewName);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    GuardAgainst.ThrowIfNull(keyPathInfo.SubKey);

                    using RegistryKey? subKey = baseKey.OpenSubKey(
                        keyPathInfo.SubKey!,
                        RegistryKeyPermissionCheck.ReadSubTree,
                        RegistryRights.QueryValues);

                    if (subKey == null)
                    {
                        continue;
                    }

                    object? defaultValue = null;
                    object? valueData = defaultValue;

                    valueData = subKey.GetValue(valueName, defaultValue, valueOptions);

                    if (valueData == null)
                    {
                        continue;
                    }

                    UnifiedLogger.Create()
                        .Message($@"Successfully retrieved value [{valueNameString}] from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}].")
                        .Severity(LogLevel.Information)
                        .Log();

                    return RegistryUtils.ConvertToType<T>(valueData, binaryValueOptions, binaryValueEncoding);
                }
                catch (SecurityException ex)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception retrieving value [{valueNameString}] from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]:{Environment.NewLine}{ex.Message}")
                        .Severity(LogLevel.Error)
                        .ErrorCategory(ErrorType.PermissionDenied)
                        .Log();

                    throw;
                }
                catch (Exception ex)
                {
                    if (_baseHiveWithViews.Count == 1)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error retrieving value [{valueNameString}] from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]:{Environment.NewLine}{ex.Message}.")
                            .Severity(LogLevel.Error)
                            .Log();
                        throw;
                    }
                    exceptions.Add(ex);
                }
                finally
                {
                    _hiveLoader?.RestartDisposeTimer();
                }
            }

            string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";
            if (exceptions.Count > 0)
            {
                UnifiedLogger.Create()
                    .Message($@"Error retrieving value [{valueNameString}] from registry key [{_keyPathInfoList[0].NormalizedKeyPath}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                    .Severity(LogLevel.Error)
                    .Log();

                throw new AggregateException(exceptions);
            }
            else
            {
                // Value not found, but no exceptions occurred
                UnifiedLogger.Create()
                    .Message($@"Value [{valueNameString}] does not exist for registry key [{_keyPathInfoList[0].NormalizedKeyPath}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                    .Severity(LogLevel.Warning)
                    .Log();
            }

            throw new ArgumentNullException(nameof(valueName));
        }

        /// <summary>
        /// Sets a value in the registry.
        /// </summary>
        /// <param name="valueName">The name of the value to set. If null, sets the default value of the subkey.</param>
        /// <param name="valueData">The data to set.</param>
        /// <param name="valueKind">The registry value kind.</param>
        /// <param name="binaryValueEncoding">The encoding for binary values.</param>
        /// <exception cref="ArgumentNullException">Thrown when valueData is null.</exception>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions.</exception>
        /// <exception cref="AggregateException">Thrown when the operation fails and multiple exceptions have occurred.</exception>
        public void SetValue(string? valueName,
                             object valueData,
                             [Optional, DefaultParameterValue(RegistryValueKind.Unknown)] RegistryValueKind valueKind,
                             [Optional, DefaultParameterValue(RegistryBinaryValueEncoding.UTF16)] RegistryBinaryValueEncoding binaryValueEncoding)
        {
            GuardAgainst.ThrowIfNull(valueData);

            string valueNameString = valueName ?? "(Default)";

            UnifiedLogger.Create()
                .Message($"Attempting to set value [{valueNameString}] in registry key [{_keyPathInfoList[0].SubKey}].")
                .Severity(LogLevel.Verbose)
                .Log();

            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();

            bool isValueSet = false;

            for (int viewCount = 0; viewCount < _baseHiveWithViews.Count; viewCount++)
            {
                RegistryKey baseKey = _baseHiveWithViews[viewCount];
                RegistryKeyInfo keyPathInfo = _keyPathInfoList[viewCount];
                viewsChecked.Add(keyPathInfo.ViewName);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    GuardAgainst.ThrowIfNull(keyPathInfo.SubKey);

                    using RegistryKey? subKey = baseKey.OpenSubKey(
                        keyPathInfo.SubKey!,
                        RegistryKeyPermissionCheck.ReadWriteSubTree,
                        RegistryRights.SetValue)
                        ?? baseKey.CreateSubKey(
                            keyPathInfo.SubKey!,
                            RegistryKeyPermissionCheck.ReadWriteSubTree);

                    if (subKey == null)
                    {
                        continue;
                    }

                    if (valueKind == RegistryValueKind.Unknown)
                    {
                        valueKind = RegistryUtils.GetValueKindFromType(valueData);
                    }

                    if (valueKind == RegistryValueKind.Binary && valueData is string stringValue)
                    {
                        valueData = RegistryUtils.ConvertStringToByteArray(stringValue, binaryValueEncoding);
                    }

                    subKey.SetValue(valueName!, valueData, valueKind);
                    isValueSet = true;

                    UnifiedLogger.Create()
                        .Message($@"Successfully set value [{valueNameString}] in registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}].")
                        .Severity(LogLevel.Information)
                        .Log();

                    break;
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception retrieving value [{valueNameString}] from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]:{Environment.NewLine}{secEx.Message}")
                        .Severity(LogLevel.Error)
                        .ErrorCategory(ErrorType.PermissionDenied)
                        .Log();

                    throw;
                }
                catch (Exception ex)
                {
                    if (_baseHiveWithViews.Count == 1)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error setting value [{valueNameString}] in registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]:{Environment.NewLine}{ex.Message}.")
                            .Severity(LogLevel.Error)
                            .Log();
                        throw;
                    }

                    exceptions.Add(ex);
                }
                finally
                {
                    _hiveLoader?.RestartDisposeTimer();
                }
            }

            if (!isValueSet && exceptions.Count > 0)
            {
                string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";
                UnifiedLogger.Create()
                    .Message($@"Error setting value [{valueNameString}] in registry key [{_keyPathInfoList[0].SubKey}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                    .Severity(LogLevel.Error)
                    .Log();

                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Removes a value from the registry.
        /// </summary>
        /// <param name="valueName">The name of the value to remove. If null, removes the default value of the subkey.</param>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions.</exception>
        /// <exception cref="AggregateException">Thrown when the operation fails and multiple exceptions have occurred.</exception>
        public void RemoveValue(string? valueName)
        {
            string valueNameString = valueName ?? "(Default)";

            UnifiedLogger.Create()
                .Message($"Attempting to remove value [{valueNameString}] from registry key [{_keyPathInfoList[0].SubKey}].")
                .Severity(LogLevel.Verbose)
                .Log();

            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();

            bool isRemovedValue = false;

            for (int viewCount = 0; viewCount < _baseHiveWithViews.Count; viewCount++)
            {
                RegistryKey baseKey = _baseHiveWithViews[viewCount];
                RegistryKeyInfo keyPathInfo = _keyPathInfoList[viewCount];
                viewsChecked.Add(keyPathInfo.ViewName);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    GuardAgainst.ThrowIfNull(keyPathInfo.SubKey);

                    using RegistryKey? subKey = baseKey.OpenSubKey(
                        keyPathInfo.SubKey!,
                        RegistryKeyPermissionCheck.ReadWriteSubTree,
                        RegistryRights.SetValue | RegistryRights.QueryValues);

                    if (subKey == null)
                    {
                        continue;
                    }

                    string[] valueNames = subKey.GetValueNames();

                    // A registry key can have a default value - that is, a name/value pair in which the name is the empty string ("").
                    // If a default value has been set for a registry key, the array returned by the GetValueNames method includes the empty string.
                    if (!valueNames.Contains(valueName ?? string.Empty))
                    {
                        continue;
                    }

                    subKey.DeleteValue(valueName!, throwOnMissingValue: true);
                    isRemovedValue = true;

                    UnifiedLogger.Create()
                        .Message($@"Successfully removed value [{valueNameString}] from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}].")
                        .Severity(LogLevel.Information)
                        .Log();

                    break;
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception occurred while removing value [{valueNameString}] from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]:{Environment.NewLine}{secEx.Message}")
                        .Severity(LogLevel.Error)
                        .Log();
                    throw;
                }
                catch (Exception ex)
                {
                    if (_baseHiveWithViews.Count == 1)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error removing value [{valueNameString}] in registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]:{Environment.NewLine}{ex.Message}.")
                            .Severity(LogLevel.Error)
                            .Log();
                        throw;
                    }
                    exceptions.Add(ex);
                }
                finally
                {
                    _hiveLoader?.RestartDisposeTimer();
                }
            }

            if (!isRemovedValue)
            {
                string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";

                if (exceptions.Count > 0)
                {
                    UnifiedLogger.Create()
                        .Message($@"Error removing value [{valueNameString}] from registry key [{_keyPathInfoList[0].NormalizedKeyPath}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                        .Severity(LogLevel.Error)
                        .Log();
                    throw new AggregateException($"Error removing registry value [{valueNameString}].", exceptions);
                }
                else
                {
                    UnifiedLogger.Create()
                        .Message($@"Value [{valueNameString}] not found in registry key [{_keyPathInfoList[0].NormalizedKeyPath}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                        .Severity(LogLevel.Warning)
                        .Log();
                }
            }
        }

        /// <summary>
        /// Tests if a value name exists in the registry.
        /// </summary>
        /// <param name="valueName">The value name to test. If this is null, it will test the default value name "(Default)" of the subkey.</param>
        /// <returns>True if the value exists; otherwise, false.</returns>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions.</exception>
        public bool TestValueName(string? valueName)
        {
            string valueNameString = valueName ?? "(Default)";

            UnifiedLogger.Create()
                .Message($"Testing existence of value name [{valueNameString}] in registry key [{_keyPathInfoList[0].SubKey}].")
                .Severity(LogLevel.Verbose)
                .Log();

            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();

            bool isValueExists = false;

            for (int viewCount = 0; viewCount < _baseHiveWithViews.Count; viewCount++)
            {
                RegistryKey baseKey = _baseHiveWithViews[viewCount];
                RegistryKeyInfo keyPathInfo = _keyPathInfoList[viewCount];
                viewsChecked.Add(keyPathInfo.ViewName);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    GuardAgainst.ThrowIfNull(keyPathInfo.SubKey);

                    using RegistryKey? subKey = baseKey.OpenSubKey(
                        keyPathInfo.SubKey!,
                        RegistryKeyPermissionCheck.ReadSubTree,
                        RegistryRights.QueryValues
                    );

                    if (subKey == null)
                    {
                        continue;
                    }

                    string[] valueNames = subKey.GetValueNames();

                    if (valueNames.Contains(valueName ?? string.Empty))
                    {
                        UnifiedLogger.Create()
                            .Message($@"Value name [{valueNameString}] exists in registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}].")
                            .Severity(LogLevel.Information)
                            .Log();
                        isValueExists = true;
                        break;
                    }
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception occurred while testing value name [{valueNameString}] from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]:{Environment.NewLine}{secEx.Message}")
                        .Severity(LogLevel.Error).ErrorCategory(ErrorType.PermissionDenied)
                        .Log();
                    throw;
                }
                catch (Exception ex)
                {
                    if (_baseHiveWithViews.Count == 1)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error testing existence of value name [{valueNameString}] in registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]:{Environment.NewLine}{ex.Message}.")
                            .Severity(LogLevel.Error)
                            .Log();
                        throw;
                    }

                    exceptions.Add(ex);
                }
                finally
                {
                    _hiveLoader?.RestartDisposeTimer();
                }
            }

            if (!isValueExists && exceptions.Count > 0)
            {
                string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";
                UnifiedLogger.Create()
                    .Message($@"Error testing existence of value name [{valueNameString}] in registry key [{_keyPathInfoList[0].NormalizedKeyPath}] with {viewOrViews} [{string.Join(", ", viewsChecked)}]")
                    .Severity(LogLevel.Warning)
                    .Log();

                throw new AggregateException("Failed to test the existence of registry value name.", exceptions);
            }

            return isValueExists;
        }

        /// <summary>
        /// Removes a registry key, optionally deleting its subkeys recursively.
        /// </summary>
        /// <param name="recurse">Whether to recursively delete subkeys. If true, all subkeys will be removed along with the key.</param>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions to delete the registry key.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the subkey is null.</exception>
        /// <exception cref="AggregateException">Thrown when the operation fails and multiple exceptions have occurred.</exception>
        public void RemoveKey(bool recurse = false)
        {
            UnifiedLogger.Create()
                .Message($"Attempting to remove registry key [{_keyPathInfoList[0].SubKey}] with recurse [{recurse}].")
                .Severity(LogLevel.Verbose)
                .Log();

            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();
            bool isKeyRemoved = false;

            for (int viewCount = 0; viewCount < _baseHiveWithViews.Count; viewCount++)
            {
                RegistryKey baseKey = _baseHiveWithViews[viewCount];
                RegistryKeyInfo keyPathInfo = _keyPathInfoList[viewCount];
                viewsChecked.Add(keyPathInfo.ViewName);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    if (keyPathInfo.SubKey == null)
                    {
                        continue;
                    }

                    if (recurse)
                    {
                        baseKey.DeleteSubKeyTree(keyPathInfo.SubKey, throwOnMissingSubKey: true);
                    }
                    else
                    {
                        baseKey.DeleteSubKey(keyPathInfo.SubKey, throwOnMissingSubKey: true);
                    }

                    UnifiedLogger.Create()
                        .Message($@"Successfully removed registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}].")
                        .Severity(LogLevel.Information)
                        .Log();

                    isKeyRemoved = true;
                    break;
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception occurred while removing registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]: {secEx.Message}")
                        .Severity(LogLevel.Error)
                        .ErrorCategory(ErrorType.PermissionDenied)
                        .Log();
                    throw;
                }
                catch (Exception ex)
                {
                    if (_baseHiveWithViews.Count == 1)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error removing registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]: {ex.Message}")
                            .Severity(LogLevel.Error)
                            .Log();

                        throw;
                    }

                    exceptions.Add(ex);
                }
                finally
                {
                    _hiveLoader?.RestartDisposeTimer();
                }
            }

            if (!isKeyRemoved)
            {
                string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";

                if (exceptions.Count > 0)
                {
                    UnifiedLogger.Create()
                        .Message($@"Error removing registry key [{_keyPathInfoList[0].NormalizedKeyPath}] with recurse [{recurse}] and {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                        .Severity(LogLevel.Error)
                        .Log();

                    throw new AggregateException($"Error removing registry key [{_keyPathInfoList[0].NormalizedKeyPath}].", exceptions);
                }
                else
                {
                    UnifiedLogger.Create()
                        .Message($@"Registry registry key [{_keyPathInfoList[0].NormalizedKeyPath}] not found with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                        .Severity(LogLevel.Warning)
                        .Log();
                }
            }
        }

        /// <summary>
        /// Creates a new registry key across different registry views.
        /// </summary>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions to create the registry key.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the subkey is null.</exception>
        /// <exception cref="AggregateException">Thrown when the operation fails and multiple exceptions have occurred.</exception>
        public void NewKey()
        {
            UnifiedLogger.Create()
                .Message($"Attempting to create new registry key [{_keyPathInfoList[0].SubKey}].")
                .Severity(LogLevel.Verbose)
                .Log();

            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();
            bool isCreatedNewKey = false;

            for (int viewCount = 0; viewCount < _baseHiveWithViews.Count; viewCount++)
            {
                RegistryKey baseKey = _baseHiveWithViews[viewCount];
                RegistryKeyInfo keyPathInfo = _keyPathInfoList[viewCount];
                viewsChecked.Add(keyPathInfo.ViewName);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    GuardAgainst.ThrowIfNull(keyPathInfo.SubKey);

                    using RegistryKey? subKey = baseKey.CreateSubKey(keyPathInfo.SubKey!, RegistryKeyPermissionCheck.ReadWriteSubTree);
                    if (subKey == null)
                    {
                        continue;
                    }

                    UnifiedLogger.Create()
                        .Message($@"Successfully created new registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}].")
                        .Severity(LogLevel.Information)
                        .Log();

                    isCreatedNewKey = true;
                    break;
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception occurred while creating registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]: {secEx.Message}")
                        .Severity(LogLevel.Error)
                        .ErrorCategory(ErrorType.PermissionDenied)
                        .Log();

                    throw;
                }
                catch (Exception ex)
                {
                    if (_baseHiveWithViews.Count == 1)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error creating new registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]: {ex.Message}")
                            .Severity(LogLevel.Error)
                            .Log();

                        throw;
                    }

                    exceptions.Add(ex);
                }
                finally
                {
                    _hiveLoader?.RestartDisposeTimer();
                }
            }

            if (!isCreatedNewKey && exceptions.Count > 0)
            {
                string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";

                UnifiedLogger.Create()
                    .Message($@"Error creating new registry key [{_keyPathInfoList[0].NormalizedKeyPath}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                    .Severity(LogLevel.Error)
                    .Log();

                throw new AggregateException($"Error creating registry key [{_keyPathInfoList[0].NormalizedKeyPath}].", exceptions);
            }
        }

        /// <summary>
        /// Tests if a registry key exists across different registry views.
        /// </summary>
        /// <returns>True if the key exists; otherwise, false.</returns>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions to access the registry key.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the subkey is null.</exception>
        /// <exception cref="AggregateException">Thrown when multiple exceptions occur while testing key existence.</exception>
        public bool TestKey()
        {
            UnifiedLogger.Create()
                .Message($"Testing existence of registry key [{_keyPathInfoList[0].SubKey}].")
                .Severity(LogLevel.Verbose)
                .Log();

            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();
            bool isKeyExists = false;

            for (int viewCount = 0; viewCount < _baseHiveWithViews.Count; viewCount++)
            {
                RegistryKey baseKey = _baseHiveWithViews[viewCount];
                RegistryKeyInfo keyPathInfo = _keyPathInfoList[viewCount];
                viewsChecked.Add(keyPathInfo.ViewName);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    GuardAgainst.ThrowIfNull(keyPathInfo.SubKey);

                    using RegistryKey? subKey = baseKey.OpenSubKey(keyPathInfo.SubKey!, RegistryKeyPermissionCheck.ReadSubTree);
                    isKeyExists = subKey != null;

                    if (isKeyExists)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Registry key [{keyPathInfo.NormalizedKeyPath}] exists with view [{keyPathInfo.ViewName}].")
                            .Severity(LogLevel.Information)
                            .Log();
                        break;
                    }
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception occurred while testing existence of registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]: {secEx.Message}")
                        .Severity(LogLevel.Error)
                        .ErrorCategory(ErrorType.PermissionDenied)
                        .Log();
                    throw;
                }
                catch (Exception ex)
                {
                    if (_baseHiveWithViews.Count == 1)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error testing existence of registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]: {ex.Message}")
                            .Severity(LogLevel.Error)
                            .Log();
                        throw;
                    }

                    exceptions.Add(ex);
                }
                finally
                {
                    _hiveLoader?.RestartDisposeTimer();
                }
            }

            if (!isKeyExists)
            {
                string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";

                if (exceptions.Count > 0)
                {
                    UnifiedLogger.Create()
                    .Message($@"Failed to test existence of registry key [{_keyPathInfoList[0].NormalizedKeyPath}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                    .Severity(LogLevel.Error)
                    .Log();

                    throw new AggregateException($"Error testing existence of registry key [{_keyPathInfoList[0].NormalizedKeyPath}].", exceptions);
                }
                else
                {
                    UnifiedLogger.Create()
                        .Message($@"Registry key [{_keyPathInfoList[0].NormalizedKeyPath}] does not exist with with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                        .Severity(LogLevel.Information)
                        .Log();
                }
            }

            return isKeyExists;
        }

        /// <summary>
        /// Gets the child value names of a registry key across different registry views.
        /// </summary>
        /// <returns>A list of child value names.</returns>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions to access the registry key.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the subkey is null.</exception>
        /// <exception cref="AggregateException">Thrown when multiple exceptions occur while retrieving the child value names.</exception>
        public List<string> GetChildValueNames()
        {
            UnifiedLogger.Create()
                .Message($"Attempting to retrieve the child value names for registry key [{_keyPathInfoList[0].SubKey}].")
                .Severity(LogLevel.Verbose)
                .Log();

            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();
            List<string> subKeyChildValueNames = new List<string>();
            bool isSubKeyChildValueNamesRetrieved = false;

            for (int viewCount = 0; viewCount < _baseHiveWithViews.Count; viewCount++)
            {
                RegistryKey baseKey = _baseHiveWithViews[viewCount];
                RegistryKeyInfo keyPathInfo = _keyPathInfoList[viewCount];
                viewsChecked.Add(keyPathInfo.ViewName);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    GuardAgainst.ThrowIfNull(keyPathInfo.SubKey);

                    using RegistryKey? subKey = baseKey.OpenSubKey(keyPathInfo.SubKey!, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.QueryValues);
                    if (subKey == null)
                    {
                        continue;
                    }

                    subKeyChildValueNames.AddRange(subKey.GetValueNames());
                    isSubKeyChildValueNamesRetrieved = true;

                    UnifiedLogger.Create()
                        .Message($@"Found [{subKeyChildValueNames.Count}] child values in registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}].")
                        .Severity(LogLevel.Information)
                        .Log();

                    break;
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception occurred while retrieving child value names from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]: {secEx.Message}")
                        .Severity(LogLevel.Error)
                        .ErrorCategory(ErrorType.PermissionDenied)
                        .Log();
                    throw;
                }
                catch (Exception ex)
                {
                    if (_baseHiveWithViews.Count == 1)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error retrieving child value names from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}]: {ex.Message}")
                            .Severity(LogLevel.Error)
                            .Log();
                        throw;
                    }

                    exceptions.Add(ex);
                }
                finally
                {
                    _hiveLoader?.RestartDisposeTimer();
                }
            }

            if (!isSubKeyChildValueNamesRetrieved)
            {
                string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";
                if (exceptions.Count > 0)
                {
                    UnifiedLogger.Create()
                        .Message($@"Error retrieving child value names from registry key [{_keyPathInfoList[0].NormalizedKeyPath}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                        .Severity(LogLevel.Error)
                        .Log();

                    throw new AggregateException($"Error retrieving child value names from registry key [{_keyPathInfoList[0].SubKey!}].", exceptions);
                }
                else
                {
                    UnifiedLogger.Create()
                        .Message($@"Registry key [{_keyPathInfoList[0].NormalizedKeyPath}] does not exist with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                        .Severity(LogLevel.Warning)
                        .Log();
                    throw new InvalidOperationException($"Registry key [{_keyPathInfoList[0].SubKey!}] does not exist.");
                }
            }

            return subKeyChildValueNames.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Copies a registry tree from a source to a destination across different registry views.
        /// </summary>
        /// <param name="destinationKeyWrapper">The destination subkey wrapper object for where the tree will be copied.</param>
        /// <exception cref="SecurityException">Thrown when the caller does not have the required permissions to copy the registry key.</exception>
        /// <exception cref="ArgumentNullException">Thrown when either the source or destination subkey is null.</exception>
        /// <exception cref="AggregateException">Thrown when multiple exceptions occur while copying the registry tree.</exception>
        public void CopyTree(CompositeRegistryKeyWrapper destinationKeyWrapper)
        {
            GuardAgainst.ThrowIfNull(_keyPathInfoList[0].SubKey);
            GuardAgainst.ThrowIfNull(destinationKeyWrapper._keyPathInfoList[0].SubKey);

            UnifiedLogger.Create()
                .Message($"Attempting to copy registry tree from [{_keyPathInfoList[0].SubKey}] to [{destinationKeyWrapper._keyPathInfoList[0].SubKey}].")
                .Severity(LogLevel.Verbose)
                .Log();

            List<Exception> exceptions = new List<Exception>();
            bool isCopied = false;
            List<string> sourceViewsChecked = new List<string>();
            HashSet<string> destinationViewsChecked = new HashSet<string>();
            int destinationViewCount;
            int sourceViewCount;
            bool isTerminate = false;

            for (int viewIndex = 0; viewIndex < _baseHiveWithViews.Count || viewIndex < destinationKeyWrapper._baseHiveWithViews.Count; viewIndex++)
            {
                sourceViewCount = viewIndex;
                if (_baseHiveWithViews.Count == 1)
                {
                    sourceViewCount = 0;
                }
                RegistryKey sourceBaseKey = _baseHiveWithViews[sourceViewCount];
                RegistryKeyInfo sourceKeyPathInfo = _keyPathInfoList[sourceViewCount];
                sourceViewsChecked.Add(sourceKeyPathInfo.ViewName);

                destinationViewCount = viewIndex;
                if (destinationKeyWrapper._baseHiveWithViews.Count == 1)
                {
                    destinationViewCount = 0;
                }
                RegistryKey destinationBaseKey = destinationKeyWrapper._baseHiveWithViews[destinationViewCount];
                RegistryKeyInfo destinationKeyPathInfo = destinationKeyWrapper._keyPathInfoList[destinationViewCount];
                destinationViewsChecked.Add(destinationKeyPathInfo.ViewName);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    if (!destinationKeyWrapper._hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        destinationKeyWrapper._hiveLoader?.PauseDisposeTimer();
                    }

                    GuardAgainst.ThrowIfNull(sourceKeyPathInfo.SubKey);
                    GuardAgainst.ThrowIfNull(destinationKeyPathInfo.SubKey);

                    using RegistryKey? sourceSubKey = sourceBaseKey.OpenSubKey(
                        sourceKeyPathInfo.SubKey!,
                        RegistryKeyPermissionCheck.ReadSubTree,
                        RegistryRights.ReadKey);

                    if (sourceSubKey == null)
                    {
                        if (_baseHiveWithViews.Count == 1 || viewIndex == 1)
                        {
                            isTerminate = true;
                            throw new InvalidOperationException($@"Source registry key [{sourceKeyPathInfo.NormalizedKeyPath}] does not exist with view [{sourceKeyPathInfo.ViewName}].");
                        }

                        continue;
                    }

                    using RegistryKey? destinationSubKey = destinationBaseKey.OpenSubKey(
                        destinationKeyPathInfo.SubKey!,
                        RegistryKeyPermissionCheck.ReadWriteSubTree,
                        RegistryRights.SetValue)
                        ?? destinationBaseKey.CreateSubKey(
                            destinationKeyPathInfo.SubKey!,
                            RegistryKeyPermissionCheck.ReadWriteSubTree);

                    if (destinationSubKey == null)
                    {
                        if (destinationKeyWrapper._baseHiveWithViews.Count == 1 || viewIndex == 1)
                        {
                            isTerminate = true;
                            throw new InvalidOperationException($@"Failed to open or create destination registry key [{destinationKeyPathInfo.SubKey}] with view [{destinationKeyPathInfo.ViewName}].");
                        }

                        continue;
                    }

                    if (!NativeMethods.RegCopyTree(sourceSubKey.Handle, null!, destinationSubKey.Handle))
                    {
                        ErrorHandler.ThrowSystemError($@"Failed to copy registry tree from [{sourceKeyPathInfo.NormalizedKeyPath}] with view [{sourceKeyPathInfo.ViewName}] to [{destinationKeyPathInfo.NormalizedKeyPath}] with view [{destinationKeyPathInfo.ViewName}].", SystemErrorType.Win32);
                    }
                    isCopied = true;

                    UnifiedLogger.Create()
                        .Message($@"Successfully copied registry tree from [{sourceKeyPathInfo.NormalizedKeyPath}] with view [{sourceKeyPathInfo.ViewName}] to [{destinationKeyPathInfo.NormalizedKeyPath}] with view [{destinationKeyPathInfo.ViewName}].")
                        .Severity(LogLevel.Information)
                        .Log();

                    break;
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception occurred while copying registry tree from [{sourceKeyPathInfo.NormalizedKeyPath}] with view [{sourceKeyPathInfo.ViewName}] to [{destinationKeyPathInfo.NormalizedKeyPath}] with view [{destinationKeyPathInfo.ViewName}]:{Environment.NewLine}{secEx.Message}")
                        .Severity(LogLevel.Error)
                        .ErrorCategory(ErrorType.PermissionDenied)
                        .Log();
                    throw;
                }
                catch (Exception ex)
                {
                    if (isTerminate || (_baseHiveWithViews.Count == 1 && destinationKeyWrapper._baseHiveWithViews.Count == 1))
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error copying registry tree from [{sourceKeyPathInfo.NormalizedKeyPath}] with view [{sourceKeyPathInfo.ViewName}] to [{destinationKeyPathInfo.NormalizedKeyPath}] with view [{destinationKeyPathInfo.ViewName}]:{Environment.NewLine}{ex.Message}")
                            .Severity(LogLevel.Error)
                            .Log();

                        throw;
                    }
                    exceptions.Add(ex);
                }
                finally
                {
                    _hiveLoader?.ResumeDisposeTimer();
                }
            }

            if (!isCopied && exceptions.Count > 0)
            {
                string sourceViewOrViews = sourceViewsChecked.Count > 1 ? "views" : "view";
                string destinationViewOrViews = destinationViewsChecked.Count > 1 ? "views" : "view";

                UnifiedLogger.Create()
                    .Message($@"Error copying registry tree from [{_keyPathInfoList[0].SubKey}] with {sourceViewOrViews} [{string.Join(", ", sourceViewsChecked)}] to [{destinationKeyWrapper._keyPathInfoList[0].SubKey}] with {destinationViewOrViews} [{string.Join(", ", destinationViewsChecked)}].")
                    .Severity(LogLevel.Error)
                    .Log();

                throw new AggregateException($"Error copying registry tree from [{_keyPathInfoList[0].SubKey!}] to [{destinationKeyWrapper._keyPathInfoList[0].SubKey!}].", exceptions);
            }

            destinationKeyWrapper?.Dispose();
        }

        /// <summary>
        /// Gets the child subkey names or paths of a registry key with a depth limit for recursion, maximum subkeys, and a timeout.
        /// </summary>
        /// <param name="recurse">Indicates whether to recurse into subkeys.</param>
        /// <param name="subKeyDisplayOptions">Specifies the display options for subkeys (relative, fully qualified, or leaf paths).</param>
        /// <param name="depthLimit">The maximum depth to traverse during recursion. Default is 0.</param>
        /// <param name="excludedPaths">A list of registry paths to avoid recursing into. Paths matching these values will be skipped.</param>
        /// <param name="maxSubKeys">The maximum number of subkeys to retrieve. Default is 1000.</param>
        /// <param name="timeout">The time limit for recursion in seconds. Default is 30 seconds.</param>
        /// <returns>A list of subkey names or paths, respecting the display options and recursion settings.</returns>
        /// <exception cref="AggregateException">Thrown when errors occur while retrieving subkeys and no subkeys are retrieved.</exception>
        public async Task<List<string>> GetChildSubKeysAsync(
            bool recurse = false,
            RegistrySubKeyDisplayOptions subKeyDisplayOptions = RegistrySubKeyDisplayOptions.RelativePath,
            uint depthLimit = 0,
            List<string>? excludedPaths = null,
            int maxSubKeys = 1000,
            uint timeout = 30)
        {
            UnifiedLogger.Create()
                .Message($"Attempting to retrieve child subkey names for registry key [{_keyPathInfoList[0].SubKey}] with recurse [{recurse}], options [{subKeyDisplayOptions}], depth limit [{depthLimit}], and excluded paths.")
                .Severity(LogLevel.Verbose)
                .Log();

            // Create a CancellationTokenSource with a timeout
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            var cancellationToken = cancellationTokenSource.Token;

            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();
            List<string> subKeys = new List<string>();
            bool isSubKeyRetrieved = false;

            for (int viewCount = 0; viewCount < _baseHiveWithViews.Count; viewCount++)
            {
                RegistryKey baseKey = _baseHiveWithViews[viewCount];
                RegistryKeyInfo keyPathInfo = _keyPathInfoList[viewCount];
                viewsChecked.Add(keyPathInfo.ViewName);

                GuardAgainst.ThrowIfNull(keyPathInfo.SubKey);

                try
                {
                    if (!_hiveLoader?.IsDisposeTimerPaused ?? true)
                    {
                        _hiveLoader?.PauseDisposeTimer();
                    }

                    // Open the initial subkey to ensure we have access
                    using RegistryKey? subKey = baseKey.OpenSubKey(keyPathInfo.SubKey!, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.EnumerateSubKeys);
                    if (subKey == null)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Registry key [{keyPathInfo.SubKey}] does not exist with view [{keyPathInfo.ViewName}].")
                            .Severity(LogLevel.Warning)
                            .Log();
                        continue;
                    }

                    // Retrieve subkeys recursively
                    subKeys.AddRange(await RetrieveSubKeysRecursiveAsync(
                        baseKey,
                        keyPathInfo,
                        recurse,
                        depthLimit,
                        excludedPaths,
                        subKeyDisplayOptions,
                        maxSubKeys,
                        cancellationToken));

                    isSubKeyRetrieved = true;
                    break;
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception occurred while accessing registry key [{keyPathInfo.SubKey}] with view [{keyPathInfo.ViewName}]:{Environment.NewLine}{secEx.Message}")
                        .Severity(LogLevel.Error)
                        .Log();
                    throw;
                }
                catch (Exception ex)
                {
                    if (_baseHiveWithViews.Count == 1)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error getting child subkey names from registry key [{keyPathInfo.NormalizedKeyPath}] with view [{keyPathInfo.ViewName}] while accessing subkey [{keyPathInfo.SubKey}]:{Environment.NewLine}{ex.Message}")
                            .Severity(LogLevel.Warning)
                            .Log();

                        throw;
                    }
                    exceptions.Add(ex);
                }
                finally
                {
                    if (isSubKeyRetrieved)
                    {
                        _hiveLoader?.RestartDisposeTimer();
                    }
                }
            }

            if (!isSubKeyRetrieved && exceptions.Count > 0)
            {
                string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";
                UnifiedLogger.Create()
                    .Message($@"Failed to get child subkeys of registry key [{_keyPathInfoList[0].SubKey}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                    .Severity(LogLevel.Error)
                    .Log();

                throw new AggregateException("Failed to get child subkeys.", exceptions);
            }

            return subKeys.OrderBy(x => x).ToList();
        }

        private static Task<List<string>> RetrieveSubKeysRecursiveAsync(
            RegistryKey baseKey,
            RegistryKeyInfo keyPathInfo,
            bool recurse,
            uint depthLimit,
            List<string>? excludedPaths,
            RegistrySubKeyDisplayOptions subKeyDisplayOptions,
            int maxSubKeys,
            CancellationToken cancellationToken)
        {
            List<string> subKeys = new List<string>();
            Queue<Dictionary<string, uint>> searchSubKeys = new Queue<Dictionary<string, uint>>();
            HashSet<string> visitedKeys = new HashSet<string>();

            // Enqueue the root subKey with a depth of 0
            searchSubKeys.Enqueue(new Dictionary<string, uint> { { keyPathInfo.SubKey!, 0 } });

            bool firstIteration = true;

            while (searchSubKeys.Count > 0 && subKeys.Count < maxSubKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentItem = searchSubKeys.Dequeue();
                var currentSubKeyPath = currentItem.Keys.First();
                var currentDepth = currentItem.Values.First(); // Use the depth stored in the queue

                // Stop if the depth limit is exceeded
                if (currentDepth > depthLimit)
                {
                    UnifiedLogger.Create()
                        .Message($@"Exceeded depth limit of [{depthLimit}] for registry key [{currentSubKeyPath}].")
                        .Severity(LogLevel.Warning)
                        .Log();
                    continue;
                }

                // Skip if the key has already been visited
                if (visitedKeys.Contains(currentSubKeyPath))
                {
                    UnifiedLogger.Create()
                        .Message($@"Skipping already visited registry path [{currentSubKeyPath}].")
                        .Severity(LogLevel.Information)
                        .Log();
                    continue;
                }

                // Skip if the path is in the excluded list
                if (excludedPaths != null && excludedPaths.Any(path => currentSubKeyPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
                {
                    UnifiedLogger.Create()
                        .Message($@"Skipping excluded registry path [{currentSubKeyPath}].")
                        .Severity(LogLevel.Information)
                        .Log();
                    continue;
                }

                // Add the current key to the visited list
                visitedKeys.Add(currentSubKeyPath);

                try
                {
                    // Open the subkey for recursion
                    using RegistryKey? currentSubKey = baseKey.OpenSubKey(currentSubKeyPath, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.EnumerateSubKeys);
                    if (currentSubKey == null)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Failed to open registry key [{currentSubKeyPath}]. It may not exist or access may be restricted.")
                            .Severity(LogLevel.Warning)
                            .Log();
                        continue;
                    }

                    // Retrieve the subkey names
                    foreach (string subKeyName in currentSubKey.GetSubKeyNames())
                    {
                        if (subKeys.Count >= maxSubKeys)
                        {
                            UnifiedLogger.Create()
                                .Message($@"Reached subkey retrieval limit of [{maxSubKeys}].")
                                .Severity(LogLevel.Warning)
                                .Log();
                            break;
                        }

                        // Enqueue for recursion if needed
                        if (recurse)
                        {
                            // Enqueue the new subkey with the incremented depth for recursion
                            searchSubKeys.Enqueue(new Dictionary<string, uint> { { $@"{currentSubKeyPath}\{subKeyName}", currentDepth + 1 } });
                        }

                        // Add formatted subkey to the result (passing both current path and subKeyName)
                        subKeys.Add(FormatSubKey(currentSubKeyPath, subKeyName, keyPathInfo, subKeyDisplayOptions, ref firstIteration));
                    }
                }
                catch (SecurityException secEx)
                {
                    UnifiedLogger.Create()
                        .Message($@"Security exception while accessing subkey [{currentSubKeyPath}]: {secEx.Message}")
                        .Severity(LogLevel.Warning)
                        .Log();
                    // Continue to next key
                }
            }

            return Task.FromResult(subKeys);
        }

        /// <summary>
        /// Formats the subkey paths based on the display options and whether it's the first iteration.
        /// </summary>
        /// <param name="currentSubKeyPath">The current path of the subkey being processed.</param>
        /// <param name="subKeyName">The name of the subkey.</param>
        /// <param name="keyPathInfo">Information about the registry key path.</param>
        /// <param name="subKeyDisplayOptions">Specifies the display options for subkeys (relative, fully qualified, or leaf paths).</param>
        /// <param name="firstIteration">Indicates whether this is the first iteration.</param>
        /// <returns>A formatted subkey name or path.</returns>
        private static string FormatSubKey(
            string currentSubKeyPath,
            string subKeyName,
            RegistryKeyInfo keyPathInfo,
            RegistrySubKeyDisplayOptions subKeyDisplayOptions,
            ref bool firstIteration)
        {
            string formattedSubKey = string.Empty;

            string fullSubKeyPath = $@"{currentSubKeyPath}\{subKeyName}";

            switch (subKeyDisplayOptions)
            {
                case RegistrySubKeyDisplayOptions.RelativePath:
                    if (!firstIteration)
                    {
                        formattedSubKey = fullSubKeyPath.StartsWith(keyPathInfo.SubKey!)
                            ? fullSubKeyPath.Substring(keyPathInfo.SubKey!.Length)
                            : fullSubKeyPath;
                    }
                    break;

                case RegistrySubKeyDisplayOptions.FullyQualifiedPath:
                    formattedSubKey = $@"{keyPathInfo.BaseKeyName}\{fullSubKeyPath}";
                    break;

                case RegistrySubKeyDisplayOptions.Leaf:
                    formattedSubKey = subKeyName; // Use only the leaf subkey name
                    break;

                default:
                    break;
            }

            firstIteration = false;

            return formattedSubKey;
        }

        /// <summary>
        /// Disposes the wrapper and its resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            lock (_syncRoot)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;

                foreach (RegistryKey baseKey in _baseHiveWithViews)
                {
                    baseKey?.Dispose();
                }
                _baseHiveWithViews.Clear();
            }

            GC.SuppressFinalize(this);
        }
    }
}
