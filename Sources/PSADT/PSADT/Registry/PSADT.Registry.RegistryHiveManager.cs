using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Win32;
using PSADT.PInvoke;
using PSADT.Logging;
using PSADT.Diagnostics.Validation;

namespace PSADT.Registry
{
    /// <summary>
    /// Manages the loading and unloading of registry hives, providing access to registry keys across different views and contexts.
    /// This class handles the lifecycle of <see cref="RegistryHiveLoader"/> instances, facilitates access to registry keys,
    /// and supports operations such as loading, unloading, and querying registry hives.
    /// </summary>
    public class RegistryHiveManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, RegistryHiveLoader> _registryHiveFileLoaders = new ConcurrentDictionary<string, RegistryHiveLoader>();
        private readonly ConcurrentDictionary<string, RegistryKey> _registryBaseHivesWithView = new ConcurrentDictionary<string, RegistryKey>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistryHiveManager"/> class.
        /// </summary>
        public RegistryHiveManager()
        {
        }

        /// <summary>
        /// Synchronous wrapper for the asynchronous method <see cref="GetRegistryKeyWrapperAsync"/>.
        /// </summary>
        /// <param name="keyPath">The registry key path.</param>
        /// <param name="views">An array of registry views to use.</param>
        /// <param name="machineName">The name of the remote machine, if any.</param>
        /// <param name="hiveFilePath">The path of the registry hive file to load, if applicable.</param>
        /// <param name="userSidMountKey">The Security Identifier (SID) of a user, if applicable.</param>
        /// <param name="localMachineMountKey">The mount point for the hive under HKEY_LOCAL_MACHINE, if applicable.</param>
        /// <returns>A <see cref="CompositeRegistryKeyWrapper"/>.</returns>
        public CompositeRegistryKeyWrapper? GetRegistryKeyWrapper(
            string keyPath,
            RegistryView[]? views = null,
            string? machineName = null,
            string hiveFilePath = "",
            string userSidMountKey = "",
            string localMachineMountKey = "")
        {
            return GetRegistryKeyWrapperAsync(keyPath, views, machineName, hiveFilePath, userSidMountKey, localMachineMountKey).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves a <see cref="CompositeRegistryKeyWrapper"/> for the specified registry key path.
        /// If <paramref name="views"/> is null or empty, it defaults to <see cref="RegistryView.Registry64"/>.
        /// </summary>
        /// <param name="keyPath">The registry key path.</param>
        /// <param name="views">An array of registry views to use. If null or empty, defaults to <see cref="RegistryView.Registry64"/>.</param>
        /// <param name="machineName">The name of the remote machine. If null, the local machine is used.</param>
        /// <param name="hiveFilePath">The path of the registry hive file to load, if applicable.</param>
        /// <param name="userSidMountKey">The Security Identifier (SID) of a user whose registry hive is to be loaded to "HKEY_USERS\{userSidMountKey}, if applicable".</param>
        /// <param name="localMachineMountKey">he mount point for the hive under HKEY_LOCAL_MACHINE, if applicable.</param>
        /// <returns>A <see cref="CompositeRegistryKeyWrapper"/> for the registry key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyPath"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the manager has been disposed.</exception>
        public async Task<CompositeRegistryKeyWrapper?> GetRegistryKeyWrapperAsync(
            string keyPath,
            RegistryView[]? views = null,
            string? machineName = null,
            string hiveFilePath = "",
            string userSidMountKey = "",
            string localMachineMountKey = "")
        {
            GuardAgainst.ThrowIfNull(keyPath);

            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(RegistryHiveManager));
            }

            views ??= new[] { RegistryView.Registry64 };
            List<Exception> exceptions = new List<Exception>();
            List<string> viewsChecked = new List<string>();
            List<RegistryKeyInfo> keyPathInfoList = new List<RegistryKeyInfo>();
            CompositeRegistryKeyWrapper? compositeWrapper = null;

            foreach (RegistryView view in views)
            {
                try
                {
                    // Build the RegistryKeyInfo object
                    var keyPathInfo = new RegistryKeyInfo.Builder()
                        .WithView(view)
                        .WithKeyPath(keyPath)
                        .WithMachineName(machineName)
                        .WithHiveFilePath(hiveFilePath)
                        .WithHiveMountPath(string.IsNullOrWhiteSpace(userSidMountKey) ? localMachineMountKey : userSidMountKey)
                        .Parse();

                    keyPathInfoList.Add(keyPathInfo);
                    viewsChecked.Add(keyPathInfo.ViewName);

                    string key = GenerateCacheKey(keyPathInfo);
                    RegistryHiveLoader? hiveLoader = null;

                    if (!_registryBaseHivesWithView.TryGetValue(key, out RegistryKey? baseHiveWithView))
                    {
                        if (!string.IsNullOrWhiteSpace(hiveFilePath) && !string.IsNullOrWhiteSpace(keyPathInfo.SubKey))
                        {
                            hiveLoader = _registryHiveFileLoaders.GetOrAdd(key, _ => new RegistryHiveLoader(keyPathInfo, TimeSpan.FromSeconds(30)));
                            await hiveLoader.LoadHiveAsync().ConfigureAwait(false);
                            hiveLoader.RestartDisposeTimer();
                        }

                        baseHiveWithView = OpenBaseKeyWithView(machineName, keyPathInfo.BaseKeyHive, view);
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(hiveFilePath) && _registryHiveFileLoaders.TryGetValue(key, out hiveLoader))
                        {
                            hiveLoader.RestartDisposeTimer();
                        }
                    }

                    if (baseHiveWithView != null)
                    {
                        if (compositeWrapper == null)
                        {
                            compositeWrapper = new CompositeRegistryKeyWrapper(hiveLoader);
                        }

                        _registryBaseHivesWithView.TryAdd(key, baseHiveWithView);
                        compositeWrapper.AddBaseHive(baseHiveWithView, keyPathInfo);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    UnifiedLogger.Create()
                        .Message($"Failed to access view [{view}] for key path [{keyPath}]:{Environment.NewLine}{ex.Message}")
                        .Severity(LogLevel.Warning)
                        .Log();
                }
            }

            if (compositeWrapper == null)
            {
                string viewOrViews = viewsChecked.Count > 1 ? "views" : "view";
                UnifiedLogger.Create()
                    .Message($"Failed to get registry key wrapper for path [{keyPath}] with {viewOrViews} [{string.Join(", ", viewsChecked)}].")
                    .Severity(LogLevel.Error)
                    .Log();
            }

            return compositeWrapper;
        }

        /// <summary>
        /// Opens the base registry key with the specified _view and machine name.
        /// </summary>
        /// <param name="machineName">The name of the machine to connect to for accessing the remote registry. If null, the local machine is used.</param>
        /// <param name="baseKeyHive">The base registry key (hive) to open.</param>
        /// <param name="view">The registry _view to use (e.g., 32-bit or 64-bit).</param>
        /// <returns>The opened <see cref="RegistryKey"/> for the specified hive and _view.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="baseKeyHive"/> is unknown.</exception>
        private static RegistryKey OpenBaseKeyWithView(string? machineName, HKEY baseKeyHive, RegistryView view)
        {
            if (baseKeyHive.Equals(HKEY.HKEY_CURRENT_USER))
            {
                return RegistryKey.OpenRemoteBaseKey(RegistryHive.CurrentUser, machineName!, view);
            }

            if (baseKeyHive.Equals(HKEY.HKEY_LOCAL_MACHINE))
            {
                return RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machineName!, view);
            }

            if (baseKeyHive.Equals(HKEY.HKEY_CLASSES_ROOT))
            {
                return RegistryKey.OpenRemoteBaseKey(RegistryHive.ClassesRoot, machineName!, view);
            }

            if (baseKeyHive.Equals(HKEY.HKEY_USERS))
            {
                return RegistryKey.OpenRemoteBaseKey(RegistryHive.Users, machineName!, view);
            }

            if (baseKeyHive.Equals(HKEY.HKEY_PERFORMANCE_DATA))
            {
                return RegistryKey.OpenRemoteBaseKey(RegistryHive.PerformanceData, machineName!, view);
            }

            if (baseKeyHive.Equals(HKEY.HKEY_CURRENT_CONFIG))
            {
                return RegistryKey.OpenRemoteBaseKey(RegistryHive.CurrentConfig, machineName!, view);
            }

            throw new ArgumentException($@"Unknown registry hive [{baseKeyHive}].");
        }

        /// <summary>
        /// Generates a cache key for the given registry key information.
        /// </summary>
        /// <param name="keyInfo">The registry key information.</param>
        /// <returns>A unique cache key.</returns>
        private static string GenerateCacheKey(RegistryKeyInfo keyInfo)
        {
            string keyComponents = $"{keyInfo.BaseKeyHive}-{keyInfo.View}-{keyInfo.MachineName ?? "local"}-{keyInfo.HiveFilePath}-{keyInfo.HiveMountPath}";

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(keyComponents);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Disposes the registry hive manager and its resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _semaphore.Wait();
            try
            {
                foreach (RegistryKey baseKey in _registryBaseHivesWithView.Values)
                {
                    baseKey?.Dispose();
                }
                _registryBaseHivesWithView.Clear();

                foreach (RegistryHiveLoader loader in _registryHiveFileLoaders.Values)
                {
                    loader?.Dispose();
                }
                _registryHiveFileLoaders.Clear();

                _isDisposed = true;
            }
            finally
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
