using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the process queries against the process running the tests.
    /// </summary>
    /// <remarks>
    /// Every one of these is a kernel query wrapped in a fallback chain, and the test host is the one
    /// process this assembly can always open, so it is the subject throughout. Where the framework can
    /// answer the same question by another route - the current process identifier, the module file name,
    /// the current identity - that answer is the oracle rather than a value written into the test.
    /// <para>
    /// Queries against processes belonging to other accounts need elevation and are covered separately.
    /// </para>
    /// </remarks>
    public sealed class ProcessUtilitiesTests
    {
        /// <summary>
        /// Verifies that the three ways of asking for a parent identifier agree for this process.
        /// </summary>
        [Fact]
        public void GetParentProcessId_AgreesAcrossItsOverloads()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act
            int fromNothing = ProcessUtilities.GetParentProcessId();
            int fromId = ProcessUtilities.GetParentProcessId(current.Id);
            int fromProcess = ProcessUtilities.GetParentProcessId(current);

            // Assert
            Assert.Equal(fromNothing, fromId);
            Assert.Equal(fromNothing, fromProcess);
            Assert.True(fromNothing > 0, "Expected a parent process identifier.");
        }

        /// <summary>
        /// Verifies that the parent identifier names a process that is really there, which is what makes
        /// it usable for anything.
        /// </summary>
        [Fact]
        public void GetParentProcess_ResolvesToALiveProcess()
        {
            // Act
            using Process parent = ProcessUtilities.GetParentProcess();

            // Assert
            Assert.Equal(ProcessUtilities.GetParentProcessId(), parent.Id);
            Assert.False(string.IsNullOrWhiteSpace(parent.ProcessName));
        }

        /// <summary>
        /// Verifies that walking up the parent chain terminates and never repeats a process.
        /// </summary>
        /// <remarks>
        /// Identifiers are reused by the operating system, so a chain walked without a guard can close
        /// into a loop. The walk keeps the ones it has seen for exactly that reason, and this asserts the
        /// guard holds rather than trusting that the machine happens not to have a cycle today.
        /// </remarks>
        [Fact]
        public void GetParentProcesses_TerminatesWithoutRepeating()
        {
            // Act
            IReadOnlyList<Process> ancestors = ProcessUtilities.GetParentProcesses();

            // Assert
            try
            {
                HashSet<int> seen = [];
                foreach (Process ancestor in ancestors)
                {
                    Assert.True(seen.Add(ancestor.Id), $"Process {ancestor.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)} appears twice in the chain.");
                }

                // The immediate parent heads the chain, so whatever else is on it, that much is known
                Assert.NotEmpty(ancestors);
                Assert.Equal(ProcessUtilities.GetParentProcessId(), ancestors[0].Id);
            }
            finally
            {
                foreach (Process ancestor in ancestors)
                {
                    ancestor.Dispose();
                }
            }
        }

        /// <summary>
        /// Verifies that this process is not reported as exited, which is the base case everything else
        /// about the check rests on.
        /// </summary>
        [Fact]
        public void HasProcessExited_ReportsThisProcessAsRunning()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act & Assert
            Assert.False(ProcessUtilities.HasProcessExited(current));
            Assert.False(ProcessUtilities.HasProcessExited(current.Id));
        }

        /// <summary>
        /// Verifies that a process that has run and finished is reported as exited.
        /// </summary>
        [Fact]
        public void HasProcessExited_ReportsAFinishedProcessAsExited()
        {
            // Arrange: a command interpreter that does nothing and returns, which changes nothing
            using Process? child = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit 0") { UseShellExecute = false, CreateNoWindow = true });
            Assert.NotNull(child);
            int childId = child.Id;
            Assert.True(child.WaitForExit(30_000), "The child process did not finish in time.");

            // Act & Assert
            Assert.True(ProcessUtilities.HasProcessExited(childId));
        }

        /// <summary>
        /// Verifies that an identifier no process holds is reported as exited rather than throwing, since
        /// a caller polling a process it launched has no other way to ask.
        /// </summary>
        [Fact]
        public void HasProcessExited_ReportsAnUnknownIdentifierAsExited()
        {
            Assert.True(ProcessUtilities.HasProcessExited(0x7FFF_FFFF));
        }

        /// <summary>
        /// Verifies that an identifier that cannot name a process is rejected as a bad argument rather
        /// than answered, so a caller passing a default value is told about it.
        /// </summary>
        /// <param name="processId">The unusable identifier.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void HasProcessExited_RejectsAnUnusableIdentifier(int processId)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => ProcessUtilities.HasProcessExited(processId));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => ProcessUtilities.GetParentProcessId(processId));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => ProcessUtilities.GetProcessSid(processId));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => ProcessUtilities.GetProcessCommandLine(processId));
        }

        /// <summary>
        /// Verifies that the owner reported for this process is the identity it is running as.
        /// </summary>
        [Fact]
        public void GetProcessSid_ReportsTheCurrentIdentity()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // Act
            SecurityIdentifier fromProcess = ProcessUtilities.GetProcessSid(current);

            // Assert
            Assert.Equal(identity.User, fromProcess);
            Assert.Equal(fromProcess, ProcessUtilities.GetProcessSid(current.Id));
        }

        /// <summary>
        /// Verifies that the command line read out of this process names the host that is running.
        /// </summary>
        /// <remarks>
        /// Read from the process block rather than from the framework, which cannot report another
        /// process's command line at all. That is the reason this wrapper exists, so the assertion is that
        /// what comes back matches what the framework can see for this process specifically.
        /// </remarks>
        [Fact]
        public void GetProcessCommandLine_ReportsTheCommandLineOfThisProcess()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? moduleName = current.MainModule?.ModuleName;
            Assert.NotNull(moduleName);

            // Act
            string commandLine = ProcessUtilities.GetProcessCommandLine(current);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(commandLine));
            Assert.Contains(moduleName, commandLine, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(commandLine, ProcessUtilities.GetProcessCommandLine(current.Id));
        }

        /// <summary>
        /// Verifies that the image name read for this process matches the module the framework reports.
        /// </summary>
        /// <remarks>
        /// This is what the fallback chain exists to produce. The query is tried five ways in turn - a
        /// kernel information class, the standard process API, a Windows XP era API, and two more
        /// information classes - because each fails in a different situation, and any of them landing on
        /// the wrong answer would be invisible without comparing against a known one.
        /// </remarks>
        [Fact]
        public void GetProcessImageName_MatchesTheModuleTheFrameworkReports()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? moduleFileName = current.MainModule?.FileName;
            Assert.NotNull(moduleFileName);

            // Act
            System.IO.FileInfo fromProcess = ProcessUtilities.GetProcessImageName(current);

            // Assert
            Assert.Equal(moduleFileName, fromProcess.FullName, ignoreCase: true);
            Assert.Equal(fromProcess.FullName, ProcessUtilities.GetProcessImageName(current.Id).FullName, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that a null process is rejected rather than dereferenced.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ProcessOverloads_RejectANullProcess()
        {
            // The two that are cast also take a handle internally, so an untyped null matches more than
            // one overload; the rest have only the process form a null can bind to.
            _ = Assert.Throws<ArgumentNullException>(static () => ProcessUtilities.GetParentProcessId((Process)null!));
            _ = Assert.Throws<ArgumentNullException>(static () => ProcessUtilities.GetProcessCommandLine((Process)null!));
            _ = Assert.Throws<ArgumentNullException>(static () => ProcessUtilities.HasProcessExited(null!));
            _ = Assert.Throws<ArgumentNullException>(static () => ProcessUtilities.GetProcessSid(null!));
            _ = Assert.Throws<ArgumentNullException>(static () => ProcessUtilities.GetProcessImageName(null!));
        }
    }
}
