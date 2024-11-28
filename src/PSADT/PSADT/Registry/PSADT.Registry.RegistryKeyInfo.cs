using System;
using System.Linq;
using Microsoft.Win32;
using PSADT.PInvoke;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.Registry
{
    /// <summary>
    /// Represents information about a registry key, including paths and registry views.
    /// This class is used for configuring and interacting with Windows registry keys.
    /// </summary>
    public class RegistryKeyInfo
    {
        // Private fields
        private HKEY _baseKeyHive;
        private readonly string _baseKeyName;
        private readonly string? _subKey;
        private readonly RegistryView _view;
        private readonly string _viewName;
        private readonly string? _machineName;
        private readonly string _hiveFilePath;
        private readonly string _hiveMountPath;
        private readonly string _normalizedKeyPath;
        private readonly string _originalKeyPath;

        /// <summary>
        /// Gets the base registry hive handle.
        /// </summary>
        public HKEY BaseKeyHive => _baseKeyHive;

        /// <summary>
        /// Gets the name of the base registry key.
        /// </summary>
        public string BaseKeyName => _baseKeyName;

        /// <summary>
        /// Gets the subkey associated with the registry key.
        /// </summary>
        public string? SubKey => _subKey;

        /// <summary>
        /// Gets the registry view (32-bit or 64-bit).
        /// </summary>
        public RegistryView View => _view;

        /// <summary>
        /// Gets the textual representation of the registry view.
        /// </summary>
        public string ViewName => _viewName;

        /// <summary>
        /// Gets the machine name where the registry key is located.
        /// </summary>
        public string? MachineName => _machineName;

        /// <summary>
        /// Gets the hive file path associated with the registry key.
        /// </summary>
        public string HiveFilePath => _hiveFilePath;

        /// <summary>
        /// Gets the mount path of the hive.
        /// </summary>
        public string HiveMountPath => _hiveMountPath;

        /// <summary>
        /// Gets the normalized registry key path.
        /// </summary>
        public string NormalizedKeyPath => _normalizedKeyPath;

        /// <summary>
        /// Gets the original registry key path before normalization.
        /// </summary>
        public string OriginalKeyPath => _originalKeyPath;

        /// <summary>
        /// Private constructor to enforce the use of the builder pattern.
        /// </summary>
        private RegistryKeyInfo(Builder builder)
        {
            _baseKeyHive = builder.BaseKeyHive;
            _baseKeyName = builder.BaseKeyName ?? throw new ArgumentNullException(nameof(builder.BaseKeyName));
            _subKey = builder.SubKey;
            _view = builder.View;
            _viewName = builder.ViewName;
            _machineName = builder.MachineName;
            _hiveFilePath = builder.HiveFilePath;
            _hiveMountPath = builder.HiveMountPath;
            _normalizedKeyPath = builder.NormalizedKeyPath ?? throw new ArgumentNullException(nameof(builder.NormalizedKeyPath));
            _originalKeyPath = builder.OriginalKeyPath ?? throw new ArgumentNullException(nameof(builder.OriginalKeyPath));
        }

        /// <summary>
        /// Updates the base registry hive with a new HKEY handle.
        /// </summary>
        /// <param name="newBaseKeyHive">The new base HKEY handle to use.</param>
        public void UpdateBaseKeyHive(HKEY newBaseKeyHive)
        {
            if (!_baseKeyHive.IsNull)
            {
                if (!NativeMethods.RegCloseKey(_baseKeyHive))
                {
                    ErrorHandler.ThrowSystemError($"Failed to close the registry hive handle.", SystemErrorType.Win32);
                }
            }
            _baseKeyHive = newBaseKeyHive;
        }

        /// <summary>
        /// Builder class for constructing instances of <see cref="RegistryKeyInfo"/>.
        /// </summary>
        public class Builder
        {
            // Properties for building RegistryKeyInfo
            public HKEY BaseKeyHive { get; private set; }
            public string BaseKeyName { get; private set; } = string.Empty;
            public string? SubKey { get; private set; }
            public RegistryView View { get; private set; } = RegistryView.Registry64;
            public string ViewName { get; private set; } = "RegistryView.Registry64";
            public string? MachineName { get; private set; }
            public string HiveFilePath { get; private set; } = string.Empty;
            public string HiveMountPath { get; private set; } = string.Empty;
            public string NormalizedKeyPath { get; private set; } = string.Empty;
            public string OriginalKeyPath { get; private set; } = string.Empty;

            /// <summary>
            /// Adds a registry view (32-bit or 64-bit).
            /// Also updates the textual representation to the ViewName property.
            /// </summary>
            /// <param name="view">The registry view to set.</param>
            /// <returns>The current <see cref="Builder"/> instance.</returns>
            public Builder WithView(RegistryView view)
            {
                View = view;
                ViewName = GetViewName(view);
                return this;
            }

            /// <summary>
            /// Sets the registry key path and normalizes it.
            /// </summary>
            /// <param name="keyPath">The original registry key path.</param>
            /// <returns>The current <see cref="Builder"/> instance.</returns>
            public Builder WithKeyPath(string keyPath)
            {
                OriginalKeyPath = keyPath;
                NormalizedKeyPath = RegistryUtils.NormalizeRegistryPath(keyPath);
                return this;
            }

            /// <summary>
            /// Sets the machine name where the registry key is located.
            /// </summary>
            /// <param name="machineName">The machine name.</param>
            /// <returns>The current <see cref="Builder"/> instance.</returns>
            public Builder WithMachineName(string? machineName)
            {
                MachineName = machineName;
                return this;
            }

            /// <summary>
            /// Sets the hive file path.
            /// </summary>
            /// <param name="hiveFilePath">The hive file path.</param>
            /// <returns>The current <see cref="Builder"/> instance.</returns>
            public Builder WithHiveFilePath(string hiveFilePath)
            {
                HiveFilePath = hiveFilePath;
                return this;
            }

            /// <summary>
            /// Sets the mount path of the hive.
            /// </summary>
            /// <param name="hiveMountPath">The hive mount path.</param>
            /// <returns>The current <see cref="Builder"/> instance.</returns>
            public Builder WithHiveMountPath(string hiveMountPath)
            {
                HiveMountPath = hiveMountPath;
                return this;
            }

            /// <summary>
            /// Parses the key path and automatically populates the remaining properties.
            /// If no views have been added via <see cref="WithView"/>, the default view of Registry64 is used.
            /// </summary>
            /// <returns>A fully constructed <see cref="RegistryKeyInfo"/> object.</returns>
            public RegistryKeyInfo Parse()
            {
                if (string.IsNullOrWhiteSpace(NormalizedKeyPath))
                {
                    throw new InvalidOperationException("Key path must be provided and normalized before parsing.");
                }

                ParseKeyPath(NormalizedKeyPath);
                return new RegistryKeyInfo(this);
            }

            /// <summary>
            /// Parses the normalized registry key path into its components and sets the necessary properties.
            /// </summary>
            private void ParseKeyPath(string keyPath)
            {
                string baseKeyName;
                string? subKey;
                HKEY baseKeyHive;

                baseKeyName = keyPath.Split('\\').FirstOrDefault() ?? string.Empty;
                baseKeyHive = GetHKEYFromName(baseKeyName);
                if (baseKeyHive.IsNull)
                {
                    throw new ArgumentException($"Invalid registry hive [{baseKeyName}].");
                }

                subKey = keyPath.Substring(baseKeyName.Length).TrimStart('\\');
                BaseKeyName = baseKeyName;
                BaseKeyHive = baseKeyHive;
                SubKey = subKey;
            }

            /// <summary>
            /// Retrieves an <see cref="HKEY"/> object from a registry hive name.
            /// </summary>
            /// <param name="hiveName">The name of the registry hive.</param>
            /// <returns>The corresponding <see cref="HKEY"/> object.</returns>
            private static HKEY GetHKEYFromName(string hiveName)
            {
                return hiveName.ToUpperInvariant() switch
                {
                    "HKEY_CLASSES_ROOT" => HKEY.HKEY_CLASSES_ROOT,
                    "HKEY_CURRENT_CONFIG" => HKEY.HKEY_CURRENT_CONFIG,
                    "HKEY_CURRENT_USER" => HKEY.HKEY_CURRENT_USER,
                    "HKEY_LOCAL_MACHINE" => HKEY.HKEY_LOCAL_MACHINE,
                    "HKEY_USERS" => HKEY.HKEY_USERS,
                    _ => HKEY.NULL
                };
            }

            /// <summary>
            /// Gets the string representation of a <see cref="RegistryView"/>.
            /// </summary>
            /// <param name="view">The registry _view.</param>
            /// <returns>The string representation of the registry view.</returns>
            private static string GetViewName(RegistryView view)
            {
                string enumName = Enum.GetName(typeof(RegistryView), view) ?? string.Empty;
                return $@"RegistryView.{enumName}";
            }
        }
    }
}
