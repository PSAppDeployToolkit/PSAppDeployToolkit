using System;
using System.Globalization;
using Microsoft.Win32;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// A file type registered for the current user whose verb runs through a DDE command, so a DDE launch can be
    /// recognised on any machine. Nothing is ever launched with it; the command it names is only there to make the
    /// registration complete.
    /// </summary>
    public sealed class DdeFileType : IDisposable
    {
        /// <summary>
        /// Registers the file type, with its DDE command under the given verb.
        /// </summary>
        /// <param name="verb">The verb that carries the DDE command. Any verb other than open is also made the type's default verb, and an open verb without DDE is registered beside it.</param>
        public DdeFileType(string verb = "open")
        {
            // A GUID rather than a counter, so it stays unique across parallel collections with no shared state.
            string id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            Extension = $".psadt-dde{id}";
            ProgId = $"PSADT.Tests.DdeFile.{id}";
            using (RegistryKey type = Registry.CurrentUser.CreateSubKey($@"{ClassesSubKeyName}\{ProgId}"))
            {
                using (RegistryKey open = type.CreateSubKey(@"shell\open\command"))
                {
                    open.SetValue(name: null, Command);
                }
                if (!verb.Equals("open", StringComparison.OrdinalIgnoreCase))
                {
                    using RegistryKey shell = type.CreateSubKey("shell");
                    shell.SetValue(name: null, verb);
                    using RegistryKey command = type.CreateSubKey($@"shell\{verb}\command");
                    command.SetValue(name: null, Command);
                }
                using RegistryKey ddeexec = type.CreateSubKey($@"shell\{verb}\ddeexec");
                ddeexec.SetValue(name: null, "[open(\"%1\")]");
            }
            using RegistryKey extension = Registry.CurrentUser.CreateSubKey($@"{ClassesSubKeyName}\{Extension}");
            extension.SetValue(name: null, ProgId);
        }

        /// <summary>
        /// The extension the type is registered for, with its leading period.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// The programmatic identifier the extension maps to.
        /// </summary>
        public string ProgId { get; }

        /// <summary>
        /// Removes the registration.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"{ClassesSubKeyName}\{Extension}", throwOnMissingSubKey: false);
                Registry.CurrentUser.DeleteSubKeyTree($@"{ClassesSubKeyName}\{ProgId}", throwOnMissingSubKey: false);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                // A key something still holds open must not turn a passing test into a failing one.
            }
        }

        private const string ClassesSubKeyName = @"Software\Classes";

        private const string Command = "\"C:\\Windows\\System32\\cmd.exe\" /c exit 0";

        private bool _disposed;
    }
}
