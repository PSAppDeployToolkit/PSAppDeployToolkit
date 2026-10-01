using System;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the facts the current process works out about itself.
    /// </summary>
    /// <remarks>
    /// The test host is 64-bit, so what a WOW64 caller works out is asked of 32-bit Windows PowerShell instead.
    /// </remarks>
    public sealed class CallerProcessInfoTests
    {
        /// <summary>
        /// Verifies that the test host reports an architecture under WOW64 exactly when the framework reports a 32-bit
        /// process on a 64-bit system.
        /// </summary>
        [Fact]
        public void Wow64Machine_MatchesWhatTheFrameworkReports()
        {
            Assert.Equal(Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess, CallerProcessInfo.Wow64Machine is not Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_UNKNOWN);
        }

        /// <summary>
        /// Verifies that a 32-bit caller reports running under WOW64 as x86.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "MA0136:Raw String contains an implicit end of line character", Justification = "The literal is PowerShell source, which parses either line ending, so the source file's choice cannot change what this does.")]
        [Fact(Skip = Wow64SkipReason, SkipUnless = nameof(TestEnvironment.CanRunUnderWow64), SkipType = typeof(TestEnvironment))]
        public async Task Wow64Machine_IsX86ForA32BitCallerAsync()
        {
            // Act: the class is internal, which a PowerShell type literal cannot name
            Wow64PowerShellResult result = await Wow64PowerShell.InvokeAsync($$"""
                $flags = [System.Reflection.BindingFlags]'NonPublic, Static'
                $type = [PSADT.ProcessManagement.ProcessUtilities].Assembly.GetType('{{typeof(CallerProcessInfo).FullName}}', $true)
                "$($type.GetProperty('{{nameof(CallerProcessInfo.Wow64Machine)}}', $flags).GetValue($null))"
                """).ConfigureAwait(true);

            // Assert
            Assert.True(result.Value is not null, result.Describe());
            Assert.Equal(nameof(Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386), result.Value);
        }

        /// <summary>
        /// Verifies that an account that is not the local system account is never reported as started by ServiceUI,
        /// since ServiceUI starts its child as that account.
        /// </summary>
        [Fact(Skip = "Requires a caller that is not the local system account.", SkipWhen = nameof(TestEnvironment.IsLocalSystem), SkipType = typeof(TestEnvironment))]
        public void UsingServiceUI_IsFalseForAnAccountThatIsNotTheSystemAccount()
        {
            Assert.False(CallerProcessInfo.UsingServiceUI);
        }

        /// <summary>
        /// The reason the WOW64 test is gated, spelled once.
        /// </summary>
        private const string Wow64SkipReason = "Requires the net472 test host on 64-bit Windows, which runs the code under test in 32-bit Windows PowerShell.";
    }
}
