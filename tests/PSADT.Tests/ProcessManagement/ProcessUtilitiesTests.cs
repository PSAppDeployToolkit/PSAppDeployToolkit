using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.FileSystem;
using PSADT.Interop;
using PSADT.ProcessManagement;
using PSADT.SafeHandles;
using PSADT.Tests.TestHelpers;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.System.Threading;
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
    /// Queries against processes belonging to other accounts need elevation and are covered separately. The
    /// exception is the kernel query that names a process by its identifier, which needs no handle: it is
    /// tested against csrss, and from 32-bit Windows PowerShell for its WOW64 branch.
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
            using Process? parent = ProcessUtilities.GetParentProcess();

            // Assert
            Assert.NotNull(parent);
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
            IReadOnlyList<int> ancestorIds = [.. ProcessUtilities.GetParentProcesses().Select(static p =>
            {
                using (p)
                {
                    return p.Id;
                }
            })];

            // Assert
            HashSet<int> seen = [];
            foreach (int ancestorId in ancestorIds)
            {
                Assert.True(seen.Add(ancestorId), $"Process {ancestorId.ToString(System.Globalization.CultureInfo.InvariantCulture)} appears twice in the chain.");
            }

            // The immediate parent heads the chain, so whatever else is on it, that much is known
            Assert.NotEmpty(ancestorIds);
            Assert.Equal(ProcessUtilities.GetParentProcessId(), ancestorIds[0]);
        }

        /// <summary>
        /// Verifies that each process in the parent chain is the parent of the one before it.
        /// </summary>
        [Fact]
        public void GetParentProcesses_ReturnsEachParentInTurn()
        {
            // Act
            IReadOnlyList<int> ancestorIds = [.. ProcessUtilities.GetParentProcesses().Select(static p =>
            {
                using (p)
                {
                    return p.Id;
                }
            })];

            // Assert
            for (int i = 1; i < ancestorIds.Count; i++)
            {
                Assert.Equal(ProcessUtilities.GetParentProcessId(ancestorIds[i - 1]), ancestorIds[i]);
            }
        }

        /// <summary>
        /// Verifies that the walk stops at a parent whose identifier Windows has given to a process younger than the
        /// child, rather than taking that process for the parent.
        /// </summary>
        [Fact]
        public void GetParentProcesses_StopsAtAParentWhoseIdentifierWasReused()
        {
            // Arrange: a child of a process started for it, which the walk goes through while that process exists
            using SuspendedProcess parent = SuspendedProcess.Start();
            using SuspendedProcess child = SuspendedProcess.Start(parent);
            int[] expectedIds = [(int)parent.Id, (int)PInvoke.GetCurrentProcessId()];
            Assert.Equal(expectedIds, GetAncestorIds(child.Handle).Take(2));

            // The parent exits, and a process started after the child is given its identifier
            parent.Dispose();
            using SuspendedProcess? reuser = SuspendedProcess.StartWithId(parent.Id, TimeSpan.FromMinutes(2));
            Assert.SkipWhen(reuser is null, "Windows did not give the exited parent's identifier to another process in time.");

            // Act & Assert
            Assert.Empty(GetAncestorIds(child.Handle));
        }

        /// <summary>
        /// Verifies that a process this host starts walks up through this host to this host's own parent, so the walk
        /// carries on past the immediate parent.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = Wow64SkipReason, SkipUnless = nameof(TestEnvironment.CanRunUnderWow64), SkipType = typeof(TestEnvironment))]
        public async Task GetParentProcesses_WalksPastTheImmediateParentAsync()
        {
            // Act
            Wow64PowerShellResult result = await Wow64PowerShell.InvokeAsync("([PSADT.ProcessManagement.ProcessUtilities]::GetParentProcesses() | Select-Object -First 2 | ForEach-Object -MemberName Id) -join ','").ConfigureAwait(true);

            // Assert
            Assert.True(result.Value is not null, result.Describe());
            Assert.Equal(PInvoke.GetCurrentProcessId().ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + ProcessUtilities.GetParentProcessId().ToString(System.Globalization.CultureInfo.InvariantCulture), result.Value);
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
        /// Verifies that a running process is still reported as running when it grants only one of the two
        /// rights the check can use, since some processes allow a limited query but refuse a wait, or the
        /// other way round.
        /// </summary>
        /// <param name="grantedAccess">The one right the process grants this account.</param>
        /// <param name="refusedAccess">The right it then refuses, which the check has to do without.</param>
        [Theory]
        [InlineData((uint)PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, (uint)PROCESS_ACCESS_RIGHTS.PROCESS_SYNCHRONIZE)]
        [InlineData((uint)PROCESS_ACCESS_RIGHTS.PROCESS_SYNCHRONIZE, (uint)PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION)]
        public void HasProcessExited_ReportsARunningProcessGrantingOneRightAsRunning(uint grantedAccess, uint refusedAccess)
        {
            // Arrange: a ping that outlives the test, whose DACL gives this account the granted right alone
            using Process? child = Process.Start(new ProcessStartInfo("ping.exe", "-n 120 127.0.0.1") { UseShellExecute = false, CreateNoWindow = true });
            Assert.NotNull(child);
            try
            {
                GrantOnly(child, (PROCESS_ACCESS_RIGHTS)grantedAccess);
                Assert.SkipWhen(CanOpen(child.Id, (PROCESS_ACCESS_RIGHTS)refusedAccess), "This account opens processes regardless of their DACL, such as with SeDebugPrivilege enabled.");

                // Act & Assert
                Assert.False(ProcessUtilities.HasProcessExited(child.Id));
            }
            finally
            {
                child.Kill();
            }
        }

        /// <summary>
        /// Verifies that a process that has run and finished is reported as exited, including one that
        /// finished with 259, which is also the exit code Windows reports for a process still running.
        /// </summary>
        /// <param name="exitCode">The code the process exits with.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(259)]
        public void HasProcessExited_ReportsAFinishedProcessAsExited(int exitCode)
        {
            // Arrange: a command interpreter that does nothing and returns, which changes nothing
            using Process? child = Process.Start(new ProcessStartInfo("cmd.exe", $"/c exit {exitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)}") { UseShellExecute = false, CreateNoWindow = true });
            Assert.NotNull(child);
            int childId = child.Id;
            Assert.True(child.WaitForExit(30_000), "The child process did not finish in time.");
            Assert.Equal(exitCode, child.ExitCode);

            // Act & Assert: the undisposed object keeps the exited process openable, so its state is read
            Assert.True(ProcessUtilities.HasProcessExited(childId));
            Assert.True(ProcessUtilities.HasProcessExited(child));
        }

        /// <summary>
        /// Verifies that a disposed process object is refused rather than answered, since once nothing
        /// holds a handle its identifier can be given to another process, so no answer could be trusted.
        /// </summary>
        [Fact]
        public void HasProcessExited_RejectsADisposedProcess()
        {
            // Arrange: disposing the object leaves this process itself running
            Process current = Process.GetCurrentProcess();
            current.Dispose();

            // Act & Assert
            _ = Assert.Throws<InvalidOperationException>(() => ProcessUtilities.HasProcessExited(current));
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
        /// Verifies that the kernel query names the test host by itself, in one call with a buffer large enough for
        /// any name.
        /// </summary>
        [Fact]
        public void QuerySystemProcessIdInformationImageName_NamesTheTestHost()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? moduleFileName = current.MainModule?.FileName;
            Assert.NotNull(moduleFileName);

            // Act
            System.IO.FileInfo imageName = QuerySystemProcessIdInformationImageName((uint)current.Id);

            // Assert
            Assert.Equal(moduleFileName, imageName.FullName, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that the kernel query refuses the system process, which has no image file, rather than inventing a
        /// path for it.
        /// </summary>
        [Fact]
        public void QuerySystemProcessIdInformationImageName_RefusesTheSystemProcess()
        {
            _ = Assert.ThrowsAny<Exception>(static () => QuerySystemProcessIdInformationImageName(SystemProcessId));
        }

        /// <summary>
        /// Verifies that a 32-bit caller names a native 64-bit process through the kernel query, which under WOW64
        /// never reports how long the name is.
        /// </summary>
        [Fact(Skip = Wow64SkipReason, SkipUnless = nameof(TestEnvironment.CanRunUnderWow64), SkipType = typeof(TestEnvironment))]
        public async Task QuerySystemProcessIdInformationImageName_NamesANative64BitProcessFromA32BitCallerAsync()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? moduleFileName = current.MainModule?.FileName;
            Assert.NotNull(moduleFileName);

            // Act
            Wow64PowerShellResult result = await Wow64PowerShell.InvokeAsync(QuerySystemProcessIdInformationImageNameScript(current.Id)).ConfigureAwait(true);

            // Assert
            Assert.True(result.Value is not null, result.Describe());
            Assert.Equal(moduleFileName, result.Value, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that a 32-bit caller names another 32-bit process through the kernel query.
        /// </summary>
        [Fact(Skip = Wow64SkipReason, SkipUnless = nameof(TestEnvironment.CanRunUnderWow64), SkipType = typeof(TestEnvironment))]
        public async Task QuerySystemProcessIdInformationImageName_NamesA32BitProcessFromA32BitCallerAsync()
        {
            // Arrange
            using RunningPing ping = await RunningPing.StartAsync(TestEnvironment.Wow64PingExecutable.FullName).ConfigureAwait(true);

            // Act
            Wow64PowerShellResult result = await Wow64PowerShell.InvokeAsync(QuerySystemProcessIdInformationImageNameScript(ping.Process.Id)).ConfigureAwait(true);

            // Assert
            Assert.True(result.Value is not null, result.Describe());
            Assert.Equal(TestEnvironment.Wow64PingExecutable.FullName, result.Value, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that a process the caller cannot open is still named, since the kernel query needs no handle and
        /// every route after it does.
        /// </summary>
        [Fact(Skip = WithoutDebugPrivilegeSkipReason, SkipWhen = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public void GetProcessImageName_NamesAProcessTheCallerCannotOpen()
        {
            // Arrange: csrss runs as the system account, so only the kernel query can name it
            int processId = GetCsrssProcessId();
            Assert.False(CanOpen(processId, PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION), "This account can open csrss, so the routes after the kernel query could answer instead.");

            // Act
            System.IO.FileInfo imageName = ProcessUtilities.GetProcessImageName(processId);

            // Assert
            Assert.Equal(CsrssImage, imageName.FullName, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that a 32-bit caller names a process it cannot open, which only the WOW64 branch of the kernel
        /// query makes possible.
        /// </summary>
        [Fact(Skip = Wow64WithoutDebugPrivilegeSkipReason, SkipUnless = nameof(TestEnvironment.CanRunUnderWow64WithoutDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public async Task GetProcessImageName_NamesAProcessA32BitCallerCannotOpenAsync()
        {
            // Arrange: csrss runs as the system account, so only the kernel query can name it
            int processId = GetCsrssProcessId();
            Assert.False(CanOpen(processId, PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION), "This account can open csrss, so the routes after the kernel query could answer instead.");

            // Act
            Wow64PowerShellResult result = await Wow64PowerShell.InvokeAsync($"[PSADT.ProcessManagement.ProcessUtilities]::GetProcessImageName([int]{processId.ToString(System.Globalization.CultureInfo.InvariantCulture)}).FullName").ConfigureAwait(true);

            // Assert
            Assert.True(result.Value is not null, result.Describe());
            Assert.Equal(CsrssImage, result.Value, ignoreCase: true);
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

        /// <summary>
        /// Verifies that the rights a handle was opened with are the rights reported back for it.
        /// </summary>
        /// <remarks>
        /// Asked of the kernel rather than remembered from the open, which is the point: a caller handed
        /// a handle by somebody else has no other way to find out what it may do with it, and attempting
        /// an operation to find out is the thing this exists to avoid.
        /// </remarks>
        [Fact]
        public void GetProcessAccessRights_ReportsWhatTheHandleWasOpenedWith()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            using SafeProcessHandle handle = current.SafeHandle;

            // Act
            PROCESS_ACCESS_RIGHTS rights = ProcessUtilities.GetProcessAccessRights(handle);

            // Assert: the framework opens its own process handle for everything, so querying is included
            Assert.NotEqual(default, rights);
            Assert.True(rights.HasFlag(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION));
        }

        /// <summary>
        /// Verifies that the process behind a running service is found, and is a process that exists.
        /// </summary>
        /// <remarks>
        /// The event log service is used because it runs on every Windows installation and cannot be
        /// stopped in normal operation, so the test needs nothing set up and starts nothing itself.
        /// <para>
        /// Whether the host has exited is deliberately not asked. A service runs as the local system
        /// account, and <see cref="ProcessUtilities.HasProcessExited(int)"/> reports a process it cannot
        /// open as gone - which for an unelevated caller is every one of them. Resolving the identifier
        /// through the framework is the stronger check regardless: it throws for an identifier no live
        /// process holds, so it succeeding is the proof the lookup found a real one.
        /// </para>
        /// </remarks>
        [Fact]
        public void GetServiceProcessId_FindsTheProcessBehindARunningService()
        {
            // Arrange
            using ServiceController service = new("EventLog");

            // Act
            uint processId = ProcessUtilities.GetServiceProcessId(service);

            // Assert
            Assert.True(processId > 0, "The event log service reported no process.");
            using Process host = Process.GetProcessById((int)processId);
            Assert.False(string.IsNullOrWhiteSpace(host.ProcessName));
        }

        /// <summary>
        /// Replaces a process's DACL with one that grants this account the given right and nothing else.
        /// </summary>
        /// <param name="process">The process to restrict, whose own handle keeps the access it was opened with.</param>
        /// <param name="access">The only right to grant.</param>
        private static void GrantOnly(Process process, PROCESS_ACCESS_RIGHTS access)
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);
            using SafePinnedGCHandle pinnedSid = SafePinnedGCHandle.Alloc(identity.User.GetBinaryForm());
            TRUSTEE_W trustee = new()
            {
                TrusteeForm = TRUSTEE_FORM.TRUSTEE_IS_SID,
                ptstrName = new(pinnedSid.DangerousGetHandle()),
            };
            EXPLICIT_ACCESS_W grant = new()
            {
                grfAccessPermissions = (uint)access,
                grfAccessMode = ACCESS_MODE.GRANT_ACCESS,
                grfInheritance = ACE_FLAGS.NO_INHERITANCE,
                Trustee = trustee,
            };
            _ = NativeMethods.SetEntriesInAcl([grant], out LocalFreeSafeHandle pAcl);
            using (pAcl)
            {
                _ = NativeMethods.SetSecurityInfo(process.SafeHandle, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, psidOwner: null, psidGroup: null, pAcl, pSacl: null);
            }
        }

        /// <summary>
        /// Walks up from a process, disposing of each parent found.
        /// </summary>
        /// <param name="hProcess">A handle to the process to walk up from.</param>
        /// <returns>The identifier of each parent, nearest first.</returns>
        private static List<int> GetAncestorIds(SafeProcessHandle hProcess)
        {
            return [.. ProcessUtilities.GetParentProcesses(hProcess).Select(static p =>
            {
                using (p)
                {
                    return p.Id;
                }
            })];
        }

        /// <summary>
        /// Determines whether this account can open a process with the given right.
        /// </summary>
        /// <param name="processId">The identifier of the process to open.</param>
        /// <param name="access">The right to open it with.</param>
        /// <returns><see langword="true"/> if the process opened; <see langword="false"/> if access was denied.</returns>
        private static bool CanOpen(int processId, PROCESS_ACCESS_RIGHTS access)
        {
            try
            {
                NativeMethods.OpenProcess(access, bInheritHandle: false, (uint)processId).Dispose();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Calls the kernel query directly, since the routes after it in <see cref="ProcessUtilities.GetProcessImageName(int)"/>
        /// would hide it failing.
        /// </summary>
        /// <param name="processId">The identifier of the process to name.</param>
        /// <returns>The image the query names.</returns>
        private static System.IO.FileInfo QuerySystemProcessIdInformationImageName(uint processId)
        {
            MethodInfo? method = typeof(ProcessUtilities).GetMethod(QuerySystemProcessIdInformationImageNameMethod, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            try
            {
                return Assert.IsType<System.IO.FileInfo>(method.Invoke(null, [processId, FileSystemUtilities.MakeNtPathLookupTable()]));
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        /// <summary>
        /// Builds the PowerShell that calls the kernel query directly for the given process.
        /// </summary>
        /// <param name="processId">The identifier of the process to name.</param>
        /// <returns>A script whose output is the image the query names.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "MA0136:Raw String contains an implicit end of line character", Justification = "The literal is PowerShell source, which parses either line ending, so the source file's choice cannot change what this does.")]
        private static string QuerySystemProcessIdInformationImageNameScript(int processId)
        {
            return $$"""
                $flags = [System.Reflection.BindingFlags]'NonPublic, Static'
                $table = [PSADT.FileSystem.FileSystemUtilities].GetMethod('{{nameof(FileSystemUtilities.MakeNtPathLookupTable)}}', $flags).Invoke($null, $null)
                $arguments = [object[]]::new(2)
                $arguments[0] = [uint32]{{processId.ToString(System.Globalization.CultureInfo.InvariantCulture)}}
                $arguments[1] = $table.psobject.BaseObject
                [PSADT.ProcessManagement.ProcessUtilities].GetMethod('{{QuerySystemProcessIdInformationImageNameMethod}}', $flags).Invoke($null, $arguments).FullName
                """;
        }

        /// <summary>
        /// Finds a csrss process, which every session has and no unprivileged account can open.
        /// </summary>
        /// <returns>The identifier of the first one found.</returns>
        private static int GetCsrssProcessId()
        {
            Process[] processes = Process.GetProcessesByName("csrss");
            try
            {
                Assert.NotEmpty(processes);
                return processes[0].Id;
            }
            finally
            {
                foreach (Process process in processes)
                {
                    process.Dispose();
                }
            }
        }

        /// <summary>
        /// The kernel query, which is private and so is reached by name.
        /// </summary>
        private const string QuerySystemProcessIdInformationImageNameMethod = "QuerySystemProcessIdInformationImageName";

        /// <summary>
        /// The identifier the system process always has.
        /// </summary>
        private const uint SystemProcessId = 4;

        /// <summary>
        /// The reason the WOW64 tests are gated, spelled once.
        /// </summary>
        private const string Wow64SkipReason = "Requires the net472 test host on 64-bit Windows, which runs the code under test in 32-bit Windows PowerShell.";

        /// <summary>
        /// The reason the WOW64 test needing an unprivileged caller is gated, spelled once.
        /// </summary>
        private const string Wow64WithoutDebugPrivilegeSkipReason = "Requires the net472 test host on 64-bit Windows, and a caller without the privilege to open every process.";

        /// <summary>
        /// The reason the test needing an unprivileged caller is gated, spelled once.
        /// </summary>
        private const string WithoutDebugPrivilegeSkipReason = "Requires a caller without the privilege to open every process.";

        /// <summary>
        /// The image every csrss process runs.
        /// </summary>
        private static readonly string CsrssImage = System.IO.Path.Join(Environment.SystemDirectory, "csrss.exe");
    }
}
