#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Text;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// Gives the test host the legacy code page encodings before any test runs.
    /// </summary>
    /// <remarks>
    /// An installer database records its strings in a code page named in its summary information, almost
    /// always 1252, and reading one asks the framework for that encoding. .NET Framework has the legacy code
    /// pages built in; .NET does not, and the host has to register a provider for them.
    /// <para>
    /// This belongs to the test host and not to the module. PowerShell registers a provider during its own
    /// startup, so the module always has them wherever it actually runs; a test host that does not is an
    /// environment the module never sees, and tests skipping against it were reporting the host's own
    /// limitation as though it were the library's.
    /// </para>
    /// </remarks>
    internal static class LegacyCodePages
    {
        /// <summary>
        /// Registers the provider as the assembly loads.
        /// </summary>
        [ModuleInitializer]
        internal static void Register()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
#endif
