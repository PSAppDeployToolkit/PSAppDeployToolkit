using System;
using System.Globalization;
using Microsoft.Win32;

namespace PSAppDeployToolkit.Tests.TestHelpers
{
    /// <summary>
    /// A uniquely named key beneath the current user's hive that is removed with the test that asked for it.
    /// </summary>
    /// <remarks>
    /// The deferral history is the one thing a session writes outside its log directory, so testing it needs
    /// somewhere writable. The current user's hive is that somewhere: it belongs to whoever is running the tests
    /// rather than to the machine, so nothing another user or another process depends on is touched.
    /// <para>
    /// The key is not created here. The code under test creates it, which is part of what is being tested; this only
    /// names it and takes it away afterwards.
    /// </para>
    /// </remarks>
    public sealed class TempRegistryKey : IDisposable
    {
        /// <summary>
        /// Names a key that does not exist yet.
        /// </summary>
        public TempRegistryKey()
        {
            // A GUID rather than a counter, so it stays unique across parallel collections with no shared state.
            SubKeyName = $@"{ParentSubKeyName}\{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
            Path = $@"HKCU:\{SubKeyName}";
        }

        /// <summary>
        /// The key's path beneath <see cref="Registry.CurrentUser"/>.
        /// </summary>
        public string SubKeyName { get; }

        /// <summary>
        /// The key's path as PowerShell names it, which is what the module's configuration carries.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Removes the key and everything beneath it.
        /// </summary>
        /// <remarks>
        /// Best-effort, and the shared parent is pruned only once it is empty, so two tests running against their
        /// own keys cannot take each other's away.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(SubKeyName, throwOnMissingSubKey: false);
                using RegistryKey? parent = Registry.CurrentUser.OpenSubKey(ParentSubKeyName);
                if (parent?.SubKeyCount is 0 && parent.ValueCount is 0)
                {
                    Registry.CurrentUser.DeleteSubKeyTree(ParentSubKeyName, throwOnMissingSubKey: false);
                }
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                // A key the code under test still holds open must not turn a passing test into a failing one.
            }
        }

        /// <summary>
        /// The one key every test's scratch key is created beneath.
        /// </summary>
        private const string ParentSubKeyName = @"SOFTWARE\PSAppDeployToolkit.Tests";

        /// <summary>
        /// Whether the key has already been removed.
        /// </summary>
        private bool _disposed;
    }
}
