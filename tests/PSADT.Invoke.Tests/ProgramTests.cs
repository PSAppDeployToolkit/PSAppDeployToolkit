using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Xunit;

namespace PSADT.Invoke.Tests
{
    /// <summary>
    /// Tests <see cref="Program"/> by running the built launcher against scripts.
    /// </summary>
    public sealed class ProgramTests
    {
        /// <summary>
        /// How long a launcher is given to exit. A guard against a hang, not an assertion about speed: a case
        /// that takes under a second on an idle machine was measured at fourteen under a parallel suite.
        /// </summary>
        private const int ProcessTimeoutMilliseconds = 120000;

        private const string DefaultMode = "Default";
        private const string DirectScriptMode = "DirectScript";
        private const string FileMode = "File";
        private const string FileCoreMode = "FileCore";
        private const string FileX86Mode = "FileX86";
        private const string InvokerFileName = "Invoke-AppDeployToolkit.exe";
        private const string PingIdFileName = "ping.pid";
        private const string ArgumentDumpFileName = "args.txt";
        private const string DebugInputFileName = "input.txt";
        private const string DebugOutputFileName = "output.txt";
        private const string DebugOutputMarker = "PowerShell wrote this.";
        private const string DebugSkipReason = "/Debug only allocates a console in an interactive session.";
        private const string PowerShellCoreSkipReason = "PowerShell 7 is not installed.";
        private const string MessageBoxSkipReason = "The help is only shown in a message box in an interactive session.";
        private const string HiddenDesktopHelperFileName = "hiddendesktop.ps1";
        private const string HiddenDesktopResultFileName = "hiddendesktop.txt";
        private const string UsageLine = "  Invoke-AppDeployToolkit.exe [/Debug] [/32] [-File <FileName>] [-DeploymentScriptParameter]";
        private static readonly int[] ScriptExitCodes = [0, 42, 3010, -1];

        /// <summary>
        /// Gets a value indicating whether this session is interactive, which /Debug needs before it allocates a console.
        /// </summary>
        public static bool IsUserInteractive => Environment.UserInteractive;

        /// <summary>
        /// Gets a value indicating whether PowerShell 7 can be found on the path.
        /// </summary>
        public static bool IsPowerShellCoreAvailable
        {
            get
            {
                using Process process = StartWhereProcess();
                return process.WaitForExit(ProcessTimeoutMilliseconds) && process.ExitCode is 0;
            }
        }

        /// <summary>
        /// Gets launcher invocation modes and expected script exit codes.
        /// </summary>
        public static TheoryData<string, int> ScriptExitCodeData
        {
            get
            {
                TheoryData<string, int> data = [];
                foreach (int exitCode in ScriptExitCodes)
                {
                    data.Add(DefaultMode, exitCode);
                    data.Add(FileMode, exitCode);
                    data.Add(DirectScriptMode, exitCode);
                    data.Add(FileX86Mode, exitCode);
                    if (IsPowerShellCoreAvailable)
                    {
                        data.Add(FileCoreMode, exitCode);
                    }
                }
                return data;
            }
        }

        /// <summary>
        /// Verifies that the launcher returns the invoked script's process exit code.
        /// </summary>
        /// <param name="invocationMode">The launcher invocation mode.</param>
        /// <param name="expectedExitCode">The expected launcher process exit code.</param>
        [Theory]
        [MemberData(nameof(ScriptExitCodeData))]
        public static void Main_ReturnsPowerShellScriptExitCode(string invocationMode, int expectedExitCode)
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, invocationMode);
            File.WriteAllText(scriptPath, GetExitScript(expectedExitCode), Encoding.UTF8);

            using Process process = StartInvoker(invokerPath, invocationMode, scriptPath);
            WaitForInvokerExit(process, invocationMode);

            Assert.Equal(expectedExitCode, process.ExitCode);
        }

        /// <summary>
        /// Verifies that the launcher writes nothing to its own output or error streams outside debug mode.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public static async Task Main_WritesNothingToOutputOrErrorAsync()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode);
            File.WriteAllText(scriptPath, GetExitScript(0), Encoding.UTF8);

            using RedirectedInvoker invoker = RedirectedInvoker.Start(invokerPath, scriptPath);
            WaitForInvokerExit(invoker.Process, DefaultMode);

            Assert.True(await ClosesOutputInTimeAsync(invoker.Process).ConfigureAwait(true), "The launcher's output streams were still open after it exited.");
            Assert.Empty(invoker.Lines);
        }

        /// <summary>
        /// Verifies that PowerShell inherits none of the launcher's handles, so a process the deployment leaves running
        /// cannot hold the launcher's output streams open.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public static async Task Main_DoesNotPassItsHandlesToPowerShellAsync()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode);
            File.WriteAllText(scriptPath, GetStartPingScript(0), Encoding.UTF8);

            using RedirectedInvoker invoker = RedirectedInvoker.Start(invokerPath, scriptPath);
            try
            {
                WaitForInvokerExit(invoker.Process, DefaultMode);
                Assert.True(await ClosesOutputInTimeAsync(invoker.Process).ConfigureAwait(true), "A process that PowerShell started is holding the launcher's output streams open.");
            }
            finally
            {
                StopPing(temporaryDirectory.DirectoryPath);
            }
        }

        /// <summary>
        /// Verifies that PowerShell runs without a console window, hidden or otherwise, outside debug mode.
        /// </summary>
        [Fact]
        public static void Main_StartsPowerShellWithoutAConsoleWindow()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode);
            File.WriteAllText(scriptPath, GetConsoleWindowScript(), Encoding.UTF8);

            using Process process = StartInvoker(invokerPath, DefaultMode, scriptPath);
            WaitForInvokerExit(process, DefaultMode);

            Assert.Equal(0, process.ExitCode);
        }

        /// <summary>
        /// Verifies that without /32 or /Core, the launcher runs the script in the PowerShell 7 that started it.
        /// </summary>
        [Fact(Skip = PowerShellCoreSkipReason, SkipUnless = nameof(IsPowerShellCoreAvailable))]
        public static void Main_RunsInThePowerShellCoreThatStartedIt()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode);
            File.WriteAllText(scriptPath, GetEditionScript(), Encoding.UTF8);

            using Process process = StartPowerShellCore($"exit (Start-Process -FilePath '{invokerPath.Replace("'", "''")}' -Wait -PassThru).ExitCode", temporaryDirectory.DirectoryPath);
            WaitForInvokerExit(process, DefaultMode);

            Assert.Equal(0, process.ExitCode);
        }

        /// <summary>
        /// Verifies that without /32 or /Core, the launcher runs the script in a PowerShell 7 further up its ancestors
        /// than its parent.
        /// </summary>
        [Fact(Skip = PowerShellCoreSkipReason, SkipUnless = nameof(IsPowerShellCoreAvailable))]
        public static void Main_RunsInAPowerShellCoreAboveItsParent()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode);
            File.WriteAllText(scriptPath, GetEditionScript(), Encoding.UTF8);
            string batchPath = Path.Join(temporaryDirectory.DirectoryPath, "StartInvoker.cmd");
            File.WriteAllText(batchPath, $"@start \"\" /wait \"%~dp0{Path.GetFileName(invokerPath)}\"{Environment.NewLine}@exit /b %errorlevel%{Environment.NewLine}", Encoding.ASCII);

            // PowerShell 7 starts cmd.exe for the batch file, which starts the launcher.
            using Process process = StartPowerShellCore($"exit (Start-Process -FilePath '{batchPath.Replace("'", "''")}' -WindowStyle Hidden -Wait -PassThru).ExitCode", temporaryDirectory.DirectoryPath);
            WaitForInvokerExit(process, DefaultMode);

            Assert.Equal(0, process.ExitCode);
        }

        /// <summary>
        /// Verifies that /32 runs the script in the x86 Windows PowerShell even when PowerShell 7 started the launcher.
        /// </summary>
        [Fact(Skip = PowerShellCoreSkipReason, SkipUnless = nameof(IsPowerShellCoreAvailable))]
        public static void Main_Runs32BitWindowsPowerShellFor32UnderPowerShellCore()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode);
            File.WriteAllText(scriptPath, Get32BitWindowsPowerShellScript(), Encoding.UTF8);

            using Process process = StartPowerShellCore($"exit (Start-Process -FilePath '{invokerPath.Replace("'", "''")}' -ArgumentList '/32' -Wait -PassThru).ExitCode", temporaryDirectory.DirectoryPath);
            WaitForInvokerExit(process, FileX86Mode);

            Assert.Equal(0, process.ExitCode);
        }

        /// <summary>
        /// Verifies that with /Debug, PowerShell runs in the console the launcher allocated and writes where it does.
        /// </summary>
        [Fact(Skip = DebugSkipReason, SkipUnless = nameof(IsUserInteractive))]
        public static void Main_RunsPowerShellInItsDebugConsole()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode);
            File.WriteAllText(scriptPath, GetDebugConsoleScript(), Encoding.UTF8);

            Assert.Equal(0, RunDebugInvoker(invokerPath, scriptPath));
            Assert.Contains(DebugOutputMarker, ReadDebugOutput(temporaryDirectory.DirectoryPath), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that with /Debug, the launcher returns PowerShell's exit code once PowerShell exits, even while a
        /// process that PowerShell started is still running.
        /// </summary>
        [Fact(Skip = DebugSkipReason, SkipUnless = nameof(IsUserInteractive))]
        public static void Main_ReturnsOnPowerShellExitInDebugMode()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode);
            File.WriteAllText(scriptPath, GetStartPingScript(42), Encoding.UTF8);

            try
            {
                Assert.Equal(42, RunDebugInvoker(invokerPath, scriptPath));
            }
            finally
            {
                StopPing(temporaryDirectory.DirectoryPath);
            }
        }

        /// <summary>
        /// Verifies that with /Debug, a Ctrl+C or Ctrl+Break in the console reaches PowerShell, even though the launcher
        /// was started ignoring Ctrl+C, but leaves the launcher waiting for its exit code.
        /// </summary>
        /// <param name="ctrlEvent">The console control event to raise, 0 for Ctrl+C and 1 for Ctrl+Break.</param>
        [Theory(Skip = DebugSkipReason, SkipUnless = nameof(IsUserInteractive))]
        [InlineData(0u)]
        [InlineData(1u)]
        public static void Main_LeavesConsoleControlEventsToPowerShellInDebugMode(uint ctrlEvent)
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            string scriptPath = GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode);
            File.WriteAllText(scriptPath, GetConsoleControlScript(ctrlEvent), Encoding.UTF8);

            Assert.Equal(0, RunDebugInvoker(invokerPath, scriptPath));
        }

        /// <summary>
        /// Verifies that with /Debug, /? or /Help writes the help to the console under the launcher's title and version,
        /// then returns 1.
        /// </summary>
        /// <param name="helpArgument">The argument asking for help.</param>
        [Theory(Skip = DebugSkipReason, SkipUnless = nameof(IsUserInteractive))]
        [InlineData("/?")]
        [InlineData("/Help")]
        [InlineData("/help")]
        public static void Main_WritesHelpInDebugMode(string helpArgument)
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);

            Assert.Equal(1, RunDebugInvokerWithArguments(invokerPath, helpArgument));
            string[] lines = File.ReadAllLines(Path.Join(temporaryDirectory.DirectoryPath, DebugOutputFileName));
            Assert.Equal(GetHelpTitle(), lines[0]);
            Assert.Contains(UsageLine, lines, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that with /Debug, /? still writes the help under the launcher's title and version when its version has
        /// no "+" and source revision, as a build made outside git has.
        /// </summary>
        [Fact(Skip = DebugSkipReason, SkipUnless = nameof(IsUserInteractive))]
        public static void Main_WritesHelpInDebugModeForAVersionWithoutASourceRevision()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            RemoveSourceRevision(invokerPath);

            Assert.Equal(1, RunDebugInvokerWithArguments(invokerPath, "/?"));
            string[] lines = File.ReadAllLines(Path.Join(temporaryDirectory.DirectoryPath, DebugOutputFileName));
            Assert.Equal(GetHelpTitle(), lines[0]);
        }

        /// <summary>
        /// Verifies that with /Debug, a failure while preparing to run the script is written to the console and returns
        /// 60010, rather than ending the launcher through FailFast.
        /// </summary>
        /// <param name="arguments">The launcher's arguments.</param>
        /// <param name="expectedMessage">Part of the message expected for the failure.</param>
        [Theory(Skip = DebugSkipReason, SkipUnless = nameof(IsUserInteractive))]
        [InlineData("-Command Get-Date", "The [-Command] parameter was specified on the command line.")]
        [InlineData("-File", "The [-File] parameter was specified without a file path.")]
        [InlineData("", "Unable to find the deployment script file at")]
        [InlineData("/32 /Core", "The use of both [/32] and [/Core] on the command line is not supported.")]
        public static void Main_ReturnsPreparationFailuresInDebugMode(string arguments, string expectedMessage)
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);

            Assert.Equal(60010, RunDebugInvokerWithArguments(invokerPath, arguments));
            Assert.Contains(expectedMessage, ReadDebugOutput(temporaryDirectory.DirectoryPath), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that with /Debug, /Core is refused with 60010 when there is no PowerShell 7 on the path, rather than
        /// ending the launcher through FailFast.
        /// </summary>
        /// <remarks>The launcher runs on a hidden desktop of WinSta0, so /Debug still allocates its console, and gets its
        /// path from the helper that starts it.</remarks>
        [Fact(Skip = DebugSkipReason, SkipUnless = nameof(IsUserInteractive))]
        public static void Main_RefusesCoreWithoutPowerShellCoreOnThePathInDebugMode()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            using TemporaryDirectory emptyDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            File.WriteAllText(GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode), GetExitScript(0), Encoding.UTF8);

            Assert.Equal(60010, RunHiddenDesktopInvoker(invokerPath, "/Debug /Core", interactive: true, emptyDirectory.DirectoryPath, Path.Join(temporaryDirectory.DirectoryPath, DebugOutputFileName)).ExitCode);
            Assert.Contains("The [/Core] parameter was specified, but PowerShell Core was not found on this system.", ReadDebugOutput(temporaryDirectory.DirectoryPath), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that /Core starts the first PowerShell 7 on the path. Neither one there can start, so the launch fails
        /// with 60011 under /Debug, naming the one the launcher chose.
        /// </summary>
        /// <remarks>The launcher runs on a hidden desktop of WinSta0, so /Debug still allocates its console, and gets its
        /// path from the helper that starts it.</remarks>
        [Fact(Skip = DebugSkipReason, SkipUnless = nameof(IsUserInteractive))]
        public static void Main_StartsTheFirstPowerShellCoreOnThePathForCore()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            using TemporaryDirectory emptyDirectory = TemporaryDirectory.Create();
            using TemporaryDirectory firstDirectory = TemporaryDirectory.Create();
            using TemporaryDirectory secondDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            File.WriteAllText(GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode), GetExitScript(0), Encoding.UTF8);
            string firstPath = Path.Join(firstDirectory.DirectoryPath, "pwsh.exe");
            File.WriteAllBytes(firstPath, []);
            File.WriteAllBytes(Path.Join(secondDirectory.DirectoryPath, "pwsh.exe"), []);
            string searchPath = $"{emptyDirectory.DirectoryPath}{Path.PathSeparator}{firstDirectory.DirectoryPath}{Path.PathSeparator}{secondDirectory.DirectoryPath}";

            Assert.Equal(60011, RunHiddenDesktopInvoker(invokerPath, "/Debug /Core", interactive: true, searchPath, Path.Join(temporaryDirectory.DirectoryPath, DebugOutputFileName)).ExitCode);
            Assert.Contains($"Error launching [{firstPath} ", ReadDebugOutput(temporaryDirectory.DirectoryPath), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that outside an interactive session, /? shows no message box and /Debug allocates no console, as
        /// nobody could answer either: the launcher returns without waiting, and /Debug runs the script as normal.
        /// </summary>
        /// <param name="arguments">The launcher's arguments.</param>
        /// <param name="expectedExitCode">The exit code expected from the launcher.</param>
        [Theory]
        [InlineData("/?", 1)]
        [InlineData("/? /Debug", 1)]
        [InlineData("/Debug", 0)]
        public static void Main_NeverWaitsOnTheUserOutsideAnInteractiveSession(string arguments, int expectedExitCode)
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            File.WriteAllText(GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode), GetConsoleWindowScript(), Encoding.UTF8);

            Assert.Equal(expectedExitCode, RunHiddenDesktopInvoker(invokerPath, arguments, interactive: false).ExitCode);
        }

        /// <summary>
        /// Verifies that in an interactive session, /? shows the help in a message box under the launcher's title and
        /// version, then returns 1 once the box is closed.
        /// </summary>
        /// <remarks>The box is shown on a desktop of WinSta0 that is never switched to, so the session stays interactive
        /// but nothing appears on screen.</remarks>
        [Fact(Skip = MessageBoxSkipReason, SkipUnless = nameof(IsUserInteractive))]
        public static void Main_ShowsTheHelpInAMessageBoxInAnInteractiveSession()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);

            (int exitCode, string messageBoxCaption, string[] messageBoxLines) = RunHiddenDesktopInvoker(invokerPath, "/?", interactive: true);

            Assert.Equal(1, exitCode);
            Assert.Equal(GetHelpTitle(), messageBoxCaption);
            Assert.Equal(GetHelpTitle(), messageBoxLines[0]);
            Assert.Contains(UsageLine, messageBoxLines, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that only the first argument can name the script, so a later one ending in .ps1 reaches the default
        /// script as a value rather than running in its place.
        /// </summary>
        [Fact]
        public static void Main_PassesALaterPs1ArgumentToTheDefaultScript()
        {
            using TemporaryDirectory temporaryDirectory = TemporaryDirectory.Create();
            string invokerPath = CopyInvokerTo(temporaryDirectory.DirectoryPath);
            File.WriteAllText(GetScriptPath(temporaryDirectory.DirectoryPath, DefaultMode), GetArgumentDumpScript(0), Encoding.UTF8);
            File.WriteAllText(Path.Join(temporaryDirectory.DirectoryPath, "Settings.ps1"), GetExitScript(99), Encoding.UTF8);

            using Process process = StartInvokerWithArguments(invokerPath, "-ConfigScript Settings.ps1");
            WaitForInvokerExit(process, "LaterPs1");

            Assert.Equal(0, process.ExitCode);
            string[] expectedArguments = ["-ConfigScript", "Settings.ps1"];
            Assert.Equal(expectedArguments, ReadArgumentDump(temporaryDirectory.DirectoryPath));
        }

        /// <summary>
        /// Gets the title the launcher shows its help under: its own title, and its version without the source revision.
        /// </summary>
        /// <returns>The title.</returns>
        private static string GetHelpTitle()
        {
            Assembly invokerAssembly = typeof(Program).Assembly;
            string informationalVersion = invokerAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
            return $"{invokerAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title} {new Version(informationalVersion.Split('+')[0])}";
        }

        /// <summary>
        /// Rewrites the informational version inside a copy of the launcher so that it has no "+" and source revision.
        /// </summary>
        /// <remarks>The version is padded with leading zeros so the file keeps its length, so 4.2.0+abc becomes 00004.2.0,
        /// which parses as the same version.</remarks>
        /// <param name="invokerPath">The copy of the launcher to rewrite.</param>
        /// <exception cref="InvalidOperationException">Thrown if the launcher has no informational version.</exception>
        private static void RemoveSourceRevision(string invokerPath)
        {
            string informationalVersion = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? throw new InvalidOperationException("The launcher has no informational version.");
            byte[] original = Encoding.UTF8.GetBytes(informationalVersion);
            byte[] replacement = Encoding.UTF8.GetBytes(informationalVersion.Split('+')[0].PadLeft(informationalVersion.Length, '0'));
            byte[] image = File.ReadAllBytes(invokerPath);
            int offset = image.AsSpan().IndexOf(original);
            Assert.True(offset != -1, $"The launcher's informational version [{informationalVersion}] was not found in [{invokerPath}].");
            Assert.True(image.AsSpan(offset + original.Length).IndexOf(original) == -1, $"The launcher's informational version [{informationalVersion}] appears more than once in [{invokerPath}].");
            replacement.CopyTo(image, offset);
            File.WriteAllBytes(invokerPath, image);
        }

        private static Process StartInvoker(string invokerPath, string invocationMode, string scriptPath)
        {
            return Process.Start(CreateInvokerStartInfo(invokerPath, invocationMode, scriptPath)) ?? throw new InvalidOperationException("Failed to start the launcher process.");
        }

        private static Process StartInvokerWithArguments(string invokerPath, string arguments)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = invokerPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetDirectoryName(invokerPath),
            };
            return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the launcher process.");
        }

        private static string[] ReadArgumentDump(string directoryPath)
        {
            return File.ReadAllLines(Path.Join(directoryPath, ArgumentDumpFileName));
        }

        private static ProcessStartInfo CreateInvokerStartInfo(string invokerPath, string invocationMode, string scriptPath)
        {
            return new()
            {
                FileName = invokerPath,
                Arguments = BuildProcessArguments(invocationMode, scriptPath),
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetDirectoryName(invokerPath),
            };
        }

        private static void WaitForInvokerExit(Process process, string invocationMode)
        {
            if (!process.WaitForExit(ProcessTimeoutMilliseconds))
            {
                string survivors = KillProcessTree(process.Id);
                Assert.Fail($"[{invocationMode}] did not exit within {ProcessTimeoutMilliseconds}ms. Killed:{Environment.NewLine}{survivors}");
            }
        }

        private static async Task<bool> ClosesOutputInTimeAsync(Process process)
        {
            // The overload without a timeout also waits for the redirected streams to close.
            Task closed = Task.Run(process.WaitForExit, TestContext.Current.CancellationToken);
            return await Task.WhenAny(closed, Task.Delay(ProcessTimeoutMilliseconds, TestContext.Current.CancellationToken)).ConfigureAwait(false) == closed;
        }

        private static int RunDebugInvoker(string invokerPath, string scriptPath)
        {
            return RunDebugInvokerWithArguments(invokerPath, BuildProcessArguments(DefaultMode, scriptPath));
        }

        private static string ReadDebugOutput(string directoryPath)
        {
            return File.ReadAllText(Path.Join(directoryPath, DebugOutputFileName));
        }

        /// <summary>
        /// Runs a launcher with /Debug, its console hidden and its input read from an empty file so that the closing key
        /// prompt returns at once, and writes its output to a file beside it.
        /// </summary>
        /// <remarks>The framework drops <c language="csharp">WindowStyle</c> outside ShellExecute, so the launcher is started
        /// through its own CreateProcess wrapper for SW_HIDE to reach the console it allocates. It starts in a new process
        /// group, which ignores Ctrl+C, so it always begins the way a parent ignoring Ctrl+C would leave it.</remarks>
        /// <param name="invokerPath">The path to the launcher.</param>
        /// <param name="arguments">The launcher's arguments after /Debug.</param>
        /// <returns>The exit code of the launcher.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the launcher's directory cannot be resolved.</exception>
        private static int RunDebugInvokerWithArguments(string invokerPath, string arguments)
        {
            string directoryPath = Path.GetDirectoryName(invokerPath) ?? throw new InvalidOperationException("Failed to resolve the launcher directory.");
            File.WriteAllText(Path.Join(directoryPath, DebugInputFileName), string.Empty);
            using FileStream input = new(Path.Join(directoryPath, DebugInputFileName), System.IO.FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Inheritable);
            using FileStream output = new(Path.Join(directoryPath, DebugOutputFileName), System.IO.FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Inheritable);

            // A wShowWindow of zero is SW_HIDE.
            STARTUPINFOW startupInfo = new()
            {
                cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
                dwFlags = STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW | STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES,
                hStdInput = (HANDLE)input.SafeFileHandle.DangerousGetHandle(),
                hStdOutput = (HANDLE)output.SafeFileHandle.DangerousGetHandle(),
                hStdError = (HANDLE)output.SafeFileHandle.DangerousGetHandle(),
            };
            _ = NativeMethods.CreateProcess(invokerPath, $"\"{invokerPath}\" /Debug {arguments}\0".ToCharArray(), bInheritHandles: true, PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP, directoryPath, in startupInfo, out PROCESS_INFORMATION pi);
            using SafeProcessHandle hProcess = new(pi.hProcess, ownsHandle: true);
            using SafeWaitHandle hThread = new(pi.hThread, ownsHandle: true);
            if (NativeMethods.WaitForSingleObject(hProcess, ProcessTimeoutMilliseconds) is not WAIT_EVENT.WAIT_OBJECT_0)
            {
                string survivors = KillProcessTree((int)pi.dwProcessId);
                Assert.Fail($"[Debug] did not exit within {ProcessTimeoutMilliseconds}ms. Killed:{Environment.NewLine}{survivors}");
            }
            _ = NativeMethods.GetExitCodeProcess(hProcess, out uint exitCode);
            return unchecked((int)exitCode);
        }

        /// <summary>
        /// Runs a launcher on a desktop nobody can see. On a window station of its own, as a deployment run as a service
        /// is, <c language="csharp">Environment.UserInteractive</c> is false for it. On WinSta0 it stays true, and a message
        /// box the launcher shows is read and closed for it.
        /// </summary>
        /// <remarks>A Windows PowerShell helper makes the desktop, as making one on another window station means switching
        /// a whole process to it for a moment. A launcher still running when the time is up is waiting for input nobody can
        /// give, and is killed.</remarks>
        /// <param name="invokerPath">The path to the launcher.</param>
        /// <param name="arguments">The launcher's arguments.</param>
        /// <param name="interactive">Whether to use a desktop of WinSta0, closing the launcher's message box.</param>
        /// <param name="searchPath">The PATH to give the launcher, or null for this process's.</param>
        /// <param name="outputPath">The file to write the launcher's output to, reading its input from an empty file so that the
        /// closing key prompt of /Debug returns at once, or null to leave both alone.</param>
        /// <returns>The exit code of the launcher, and the caption and lines of the message box it showed, which are empty
        /// if it showed none.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the launcher's directory cannot be resolved, or Windows PowerShell cannot be started.</exception>
        private static (int ExitCode, string MessageBoxCaption, string[] MessageBoxLines) RunHiddenDesktopInvoker(string invokerPath, string arguments, bool interactive, string? searchPath = null, string? outputPath = null)
        {
            string directoryPath = Path.GetDirectoryName(invokerPath) ?? throw new InvalidOperationException("Failed to resolve the launcher directory.");
            string helperPath = Path.Join(directoryPath, HiddenDesktopHelperFileName);
            string resultPath = Path.Join(directoryPath, HiddenDesktopResultFileName);
            File.WriteAllText(helperPath, GetHiddenDesktopHelperScript(), Encoding.UTF8);
            string searchPathArgument = searchPath is null ? string.Empty : $" -SearchPath \"{searchPath}\"";
            string outputPathArgument = outputPath is null ? string.Empty : $" -OutputPath \"{outputPath}\"";
            ProcessStartInfo startInfo = new()
            {
                FileName = Path.Join(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{helperPath}\" -InvokerPath \"{invokerPath}\" -Arguments \"{arguments}\" -ResultPath \"{resultPath}\" -TimeoutMilliseconds {ProcessTimeoutMilliseconds}{(interactive ? " -Interactive" : string.Empty)}{searchPathArgument}{outputPathArgument}",
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = directoryPath,
            };
            StringBuilder output = new();
            void AppendLine(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _ = output.AppendLine(e.Data);
                }
            }

            using Process helper = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Windows PowerShell.");
            helper.ErrorDataReceived += AppendLine;
            helper.OutputDataReceived += AppendLine;
            helper.BeginErrorReadLine();
            helper.BeginOutputReadLine();
            if (!helper.WaitForExit(ProcessTimeoutMilliseconds * 2))
            {
                string survivors = KillProcessTree(helper.Id);
                Assert.Fail($"[{arguments}] The helper did not exit within {ProcessTimeoutMilliseconds * 2}ms. Killed:{Environment.NewLine}{survivors}");
            }

            // The overload taking a timeout returns before the redirected streams have finished.
            helper.WaitForExit();
            if (!File.Exists(resultPath))
            {
                Assert.Fail($"[{arguments}] The helper did not run the launcher:{Environment.NewLine}{output}");
            }
            // The first line is the exit code, or "TimedOut" and the process ID; the message box's caption and text follow.
            string[] result = File.ReadAllLines(resultPath);
            string[] outcome = result[0].Split(' ');
            if (outcome[0].Equals("TimedOut", StringComparison.Ordinal))
            {
                string survivors = KillProcessTree(int.Parse(outcome[1], CultureInfo.InvariantCulture));
                Assert.Fail($"[{arguments}] did not exit within {ProcessTimeoutMilliseconds}ms. {result[1]}{Environment.NewLine}Killed:{Environment.NewLine}{survivors}");
            }
            return (int.Parse(outcome[0], CultureInfo.InvariantCulture), result[1], [.. result.Skip(2)]);
        }

        private static string BuildProcessArguments(string invocationMode, string scriptPath)
        {
            string[] arguments = invocationMode switch
            {
                DefaultMode => [],
                DirectScriptMode => [scriptPath],
                FileMode => ["-File", scriptPath],
                FileCoreMode => ["/Core", "-File", scriptPath],
                FileX86Mode => ["/32", "-File", scriptPath],
                _ => throw new ArgumentOutOfRangeException(nameof(invocationMode), invocationMode, "Unsupported invocation mode."),
            };
            return string.Join(" ", arguments.Select(QuoteArgument));
        }

        private static string CopyInvokerTo(string directoryPath)
        {
            string sourcePath = GetInvokerPath();
            string sourceDirectoryPath = Path.GetDirectoryName(sourcePath) ?? throw new InvalidOperationException("Failed to resolve the launcher output directory.");
            string sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourcePath);
            foreach (string sourceFilePath in Directory.EnumerateFiles(sourceDirectoryPath, sourceFileNameWithoutExtension + ".*"))
            {
                File.Copy(sourceFilePath, Path.Join(directoryPath, Path.GetFileName(sourceFilePath)));
            }
            return Path.Join(directoryPath, InvokerFileName);
        }

        /// <summary>
        /// Terminates a process and everything it started.
        /// </summary>
        /// <remarks>
        /// No job object ties the launcher to the PowerShell it starts, so <c language="csharp">Process.Kill</c> takes
        /// only the launcher. An abandoned child holds the test's temporary directory open and competes for the runner
        /// for the rest of the job. The .NET Framework has no entireProcessTree overload, so the walk is taskkill's.
        /// </remarks>
        /// <param name="processId">The identifier of the process at the root of the tree.</param>
        /// <returns>
        /// What taskkill reported, which names every process it found. A timeout otherwise says only that the
        /// launcher did not exit, where the useful question is whether it ever reached starting PowerShell.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if taskkill cannot be started.</exception>
        private static string KillProcessTree(int processId)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "taskkill.exe",
                Arguments = "/T /F /PID " + processId.ToString(CultureInfo.InvariantCulture),
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            StringBuilder output = new();
            void AppendLine(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _ = output.AppendLine(e.Data);
                }
            }

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start taskkill.exe.");
            process.ErrorDataReceived += AppendLine;
            process.OutputDataReceived += AppendLine;
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            if (process.WaitForExit(ProcessTimeoutMilliseconds))
            {
                // The overload taking a timeout returns before the redirected streams have finished.
                process.WaitForExit();
            }
            return output.ToString().Trim();
        }

        private static string GetExitScript(int exitCode)
        {
            return "exit " + exitCode.ToString(CultureInfo.InvariantCulture) + Environment.NewLine;
        }

        /// <summary>
        /// Gets a script that writes each argument it was passed to a file beside it, one per line, then exits.
        /// </summary>
        /// <param name="exitCode">The exit code for the script to exit with.</param>
        /// <returns>The script source.</returns>
        private static string GetArgumentDumpScript(int exitCode)
        {
            return string.Join(
                Environment.NewLine,
                $"[System.IO.File]::WriteAllLines((Join-Path -Path $PSScriptRoot -ChildPath '{ArgumentDumpFileName}'), [System.String[]]$args)",
                GetExitScript(exitCode));
        }

        /// <summary>
        /// Gets a script that leaves ping running with every inheritable handle PowerShell has, records its ID, then exits.
        /// </summary>
        /// <param name="exitCode">The exit code for the script to exit with.</param>
        /// <returns>The script source.</returns>
        private static string GetStartPingScript(int exitCode)
        {
            return string.Join(
                Environment.NewLine,
                "$startInfo = [System.Diagnostics.ProcessStartInfo]::new((Join-Path -Path ([System.Environment]::SystemDirectory) -ChildPath 'PING.EXE'), '-n 600 127.0.0.1')",
                "$startInfo.UseShellExecute = $false",
                "$startInfo.WorkingDirectory = [System.Environment]::SystemDirectory",
                $"[System.IO.File]::WriteAllText((Join-Path -Path $PSScriptRoot -ChildPath '{PingIdFileName}'), [System.Diagnostics.Process]::Start($startInfo).Id)",
                GetExitScript(exitCode));
        }

        /// <summary>
        /// Gets a script that exits with 2 if PowerShell has a console window, as an error alone exits with 1.
        /// </summary>
        /// <returns>The script source.</returns>
        private static string GetConsoleWindowScript()
        {
            return string.Join(
                Environment.NewLine,
                "$ErrorActionPreference = 'Stop'",
                "Add-Type -Namespace PSADT.Invoke.Tests -Name ConsoleWindowProbe -MemberDefinition '[System.Runtime.InteropServices.DllImport(\"kernel32.dll\")] public static extern System.IntPtr GetConsoleWindow();'",
                "if ([PSADT.Invoke.Tests.ConsoleWindowProbe]::GetConsoleWindow() -ne [System.IntPtr]::Zero)",
                "{",
                "    exit 2",
                "}",
                "exit 0",
                "");
        }

        /// <summary>
        /// Gets a script that exits with 2 unless it runs in PowerShell 7, as an error alone exits with 1.
        /// </summary>
        /// <returns>The script source.</returns>
        private static string GetEditionScript()
        {
            return string.Join(
                Environment.NewLine,
                "if ($PSVersionTable.PSEdition -ne 'Core')",
                "{",
                "    exit 2",
                "}",
                "exit 0",
                "");
        }

        /// <summary>
        /// Gets a script that exits with 2 unless it runs in a 32-bit Windows PowerShell, as an error alone exits with 1.
        /// </summary>
        /// <returns>The script source.</returns>
        private static string Get32BitWindowsPowerShellScript()
        {
            return string.Join(
                Environment.NewLine,
                "if ($PSVersionTable.PSEdition -ne 'Desktop' -or [System.Environment]::Is64BitProcess)",
                "{",
                "    exit 2",
                "}",
                "exit 0",
                "");
        }

        /// <summary>
        /// Gets a Windows PowerShell script that runs a launcher on a new desktop nobody can see, then writes its exit code,
        /// or "TimedOut" and its process ID if it is still running when the time is up, to a file. With -Interactive, the
        /// desktop is on WinSta0, and the caption and text of a message box the launcher shows follow, the box closed for it.
        /// </summary>
        /// <remarks>Only WinSta0 is ever visible, and only its active desktop is on screen. Without -Interactive,
        /// CreateWindowStation gets no name, which only an administrator may give, so it makes or opens the window station
        /// of the caller's logon session.</remarks>
        /// <returns>The script source.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "MA0136:Raw String contains an implicit end of line character", Justification = "The literal is PowerShell source with C# inside it, both of which parse either line ending, so the source file's choice cannot change what this does.")]
        private static string GetHiddenDesktopHelperScript()
        {
            return """
                param ([System.String]$InvokerPath, [System.String]$Arguments, [System.String]$ResultPath, [System.UInt32]$TimeoutMilliseconds, [System.Management.Automation.SwitchParameter]$Interactive, [System.String]$SearchPath, [System.String]$OutputPath)
                $ErrorActionPreference = 'Stop'
                Add-Type -TypeDefinition @'
                using System;
                using System.ComponentModel;
                using System.Globalization;
                using System.IO;
                using System.Runtime.InteropServices;
                using System.Text;
                using System.Threading;

                namespace PSADT.Invoke.Tests
                {
                    public static class HiddenDesktop
                    {
                        private const uint WinStaAllAccess = 0x37F;
                        private const uint GenericAll = 0x10000000;
                        private const int UoiName = 2;
                        private const uint WaitTimeout = 0x102;
                        private const uint WmClose = 0x10;
                        private const int MessageBoxTextId = 0xFFFF;
                        private const int StartfUseStdHandles = 0x100;
                        private const uint HandleFlagInherit = 1;
                        private static readonly int[] StandardHandles = new int[] { -10, -11, -12 };

                        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

                        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
                        private struct STARTUPINFO
                        {
                            public int cb;
                            public string lpReserved;
                            public string lpDesktop;
                            public string lpTitle;
                            public int dwX;
                            public int dwY;
                            public int dwXSize;
                            public int dwYSize;
                            public int dwXCountChars;
                            public int dwYCountChars;
                            public int dwFillAttribute;
                            public int dwFlags;
                            public short wShowWindow;
                            public short cbReserved2;
                            public IntPtr lpReserved2;
                            public IntPtr hStdInput;
                            public IntPtr hStdOutput;
                            public IntPtr hStdError;
                        }

                        [StructLayout(LayoutKind.Sequential)]
                        private struct PROCESS_INFORMATION
                        {
                            public IntPtr hProcess;
                            public IntPtr hThread;
                            public int dwProcessId;
                            public int dwThreadId;
                        }

                        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
                        private static extern IntPtr CreateWindowStation(string lpwinsta, uint dwFlags, uint dwDesiredAccess, IntPtr lpsa);

                        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
                        private static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex, StringBuilder pvInfo, int nLength, out int lpnLengthNeeded);

                        [DllImport("user32.dll", SetLastError = true)]
                        private static extern IntPtr GetProcessWindowStation();

                        [DllImport("user32.dll", SetLastError = true)]
                        private static extern bool SetProcessWindowStation(IntPtr hWinSta);

                        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
                        private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice, IntPtr pDevmode, uint dwFlags, uint dwDesiredAccess, IntPtr lpsa);

                        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
                        private static extern bool CreateProcess(string lpApplicationName, StringBuilder lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

                        [DllImport("kernel32.dll", SetLastError = true)]
                        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

                        [DllImport("kernel32.dll", SetLastError = true)]
                        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

                        [DllImport("user32.dll", SetLastError = true)]
                        private static extern bool SetThreadDesktop(IntPtr hDesktop);

                        [DllImport("user32.dll")]
                        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

                        [DllImport("user32.dll")]
                        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

                        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
                        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

                        [DllImport("user32.dll")]
                        private static extern bool IsWindowVisible(IntPtr hWnd);

                        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
                        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

                        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
                        private static extern uint GetDlgItemText(IntPtr hDlg, int nIDDlgItem, StringBuilder lpString, int nMaxCount);

                        [DllImport("user32.dll", SetLastError = true)]
                        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

                        [DllImport("kernel32.dll", SetLastError = true)]
                        private static extern IntPtr GetStdHandle(int nStdHandle);

                        [DllImport("kernel32.dll", SetLastError = true)]
                        private static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);

                        public static string[] Run(string applicationName, string commandLine, uint timeoutMilliseconds, bool interactive, string outputPath)
                        {
                            IntPtr windowStation = interactive ? GetProcessWindowStation() : CreateWindowStation(null, 0, WinStaAllAccess, IntPtr.Zero);
                            StringBuilder windowStationName = new StringBuilder(256);
                            int lengthNeeded;
                            if (windowStation == IntPtr.Zero || !GetUserObjectInformation(windowStation, UoiName, windowStationName, windowStationName.Capacity * 2, out lengthNeeded))
                            {
                                throw new Win32Exception();
                            }

                            // A desktop is made on the window station of the calling process, so switch to it just for that.
                            string desktopName = "PSADT.Invoke.Tests." + Guid.NewGuid().ToString("N");
                            IntPtr processWindowStation = GetProcessWindowStation();
                            if (!SetProcessWindowStation(windowStation))
                            {
                                throw new Win32Exception();
                            }
                            IntPtr desktop = CreateDesktop(desktopName, IntPtr.Zero, IntPtr.Zero, 0, GenericAll, IntPtr.Zero);
                            int desktopError = Marshal.GetLastWin32Error();
                            if (!SetProcessWindowStation(processWindowStation))
                            {
                                throw new Win32Exception();
                            }
                            if (desktop == IntPtr.Zero)
                            {
                                throw new Win32Exception(desktopError);
                            }

                            // The handles close when this helper exits, after the launcher is done with them.
                            STARTUPINFO startupInfo = new STARTUPINFO();
                            startupInfo.cb = Marshal.SizeOf(typeof(STARTUPINFO));
                            startupInfo.lpDesktop = windowStationName + "\\" + desktopName;

                            // Input from an empty file makes the closing key prompt of /Debug return at once. Only these files are
                            // inherited, not this helper's own standard handles.
                            FileStream input = null;
                            FileStream output = null;
                            if (outputPath.Length > 0)
                            {
                                File.WriteAllText(outputPath + ".input", string.Empty);
                                input = new FileStream(outputPath + ".input", FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Inheritable);
                                output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Inheritable);
                                foreach (int standardHandle in StandardHandles)
                                {
                                    SetHandleInformation(GetStdHandle(standardHandle), HandleFlagInherit, 0);
                                }
                                startupInfo.dwFlags = StartfUseStdHandles;
                                startupInfo.hStdInput = input.SafeFileHandle.DangerousGetHandle();
                                startupInfo.hStdOutput = output.SafeFileHandle.DangerousGetHandle();
                                startupInfo.hStdError = startupInfo.hStdOutput;
                            }
                            PROCESS_INFORMATION processInformation;
                            if (!CreateProcess(applicationName, new StringBuilder(commandLine), IntPtr.Zero, IntPtr.Zero, output != null, 0, IntPtr.Zero, null, ref startupInfo, out processInformation))
                            {
                                throw new Win32Exception();
                            }
                            if (output != null)
                            {
                                // The launcher has its own copies of the handles now.
                                input.Dispose();
                                output.Dispose();
                            }

                            // Messages only reach a window from a thread on its desktop, so the message box is closed from one.
                            string[] messageBox = new string[] { string.Empty, string.Empty };
                            Thread closer = new Thread(delegate () { CloseMessageBox(desktop, processInformation.hProcess, (uint)processInformation.dwProcessId, messageBox); });
                            closer.IsBackground = true;
                            if (interactive)
                            {
                                closer.Start();
                            }
                            uint waitResult = WaitForSingleObject(processInformation.hProcess, timeoutMilliseconds);
                            if (waitResult == WaitTimeout)
                            {
                                return new string[] { "TimedOut " + processInformation.dwProcessId.ToString(CultureInfo.InvariantCulture), messageBox[0], messageBox[1] };
                            }
                            uint exitCode;
                            if (waitResult != 0 || !GetExitCodeProcess(processInformation.hProcess, out exitCode))
                            {
                                throw new Win32Exception();
                            }
                            if (interactive)
                            {
                                closer.Join();
                            }
                            return new string[] { unchecked((int)exitCode).ToString(CultureInfo.InvariantCulture), messageBox[0], messageBox[1] };
                        }

                        private static void CloseMessageBox(IntPtr desktop, IntPtr process, uint processId, string[] messageBox)
                        {
                            // An exception here would end the helper without a result, so it is reported in the caption instead.
                            try
                            {
                                if (!SetThreadDesktop(desktop))
                                {
                                    throw new Win32Exception();
                                }
                                while (WaitForSingleObject(process, 50) == WaitTimeout)
                                {
                                    IntPtr found = IntPtr.Zero;
                                    EnumWindows(delegate (IntPtr window, IntPtr parameter)
                                    {
                                        uint windowProcessId;
                                        GetWindowThreadProcessId(window, out windowProcessId);
                                        StringBuilder className = new StringBuilder(16);
                                        GetClassName(window, className, className.Capacity);
                                        if (windowProcessId == processId && IsWindowVisible(window) && className.ToString() == "#32770")
                                        {
                                            found = window;
                                            return false;
                                        }
                                        return true;
                                    }, IntPtr.Zero);
                                    if (found != IntPtr.Zero)
                                    {
                                        StringBuilder text = new StringBuilder(8192);
                                        GetWindowText(found, text, text.Capacity);
                                        messageBox[0] = text.ToString();
                                        text.Length = 0;
                                        GetDlgItemText(found, MessageBoxTextId, text, text.Capacity);
                                        messageBox[1] = text.ToString();

                                        // The only button of an MB_OK box has the ID IDCANCEL, not IDOK, so close the box instead.
                                        if (!PostMessage(found, WmClose, IntPtr.Zero, IntPtr.Zero))
                                        {
                                            throw new Win32Exception();
                                        }
                                        return;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                messageBox[0] = "Closing the message box failed: " + ex.Message;
                            }
                        }
                    }
                }
                '@
                if ($SearchPath)
                {
                    $env:PATH = $SearchPath
                }
                [System.IO.File]::WriteAllLines($ResultPath, [System.String[]][PSADT.Invoke.Tests.HiddenDesktop]::Run($InvokerPath, ('"{0}" {1}' -f $InvokerPath, $Arguments), $TimeoutMilliseconds, $Interactive.IsPresent, $OutputPath))
                """;
        }

        /// <summary>
        /// Gets a script that writes <see cref="DebugOutputMarker"/>, then exits with 2 if the launcher is not attached to
        /// PowerShell's console, as an error alone exits with 1.
        /// </summary>
        /// <returns>The script source.</returns>
        private static string GetDebugConsoleScript()
        {
            return string.Join(
                Environment.NewLine,
                "$ErrorActionPreference = 'Stop'",
                $"'{DebugOutputMarker}'",
                "Add-Type -Namespace PSADT.Invoke.Tests -Name ConsoleProcessProbe -MemberDefinition '[System.Runtime.InteropServices.DllImport(\"kernel32.dll\")] public static extern uint GetConsoleProcessList(uint[] processList, uint processCount);'",
                "$processIds = [System.UInt32[]]::new(64)",
                "$count = [PSADT.Invoke.Tests.ConsoleProcessProbe]::GetConsoleProcessList($processIds, $processIds.Length)",
                "$names = foreach ($processId in $processIds[0..($count - 1)])",
                "{",
                "    (Get-Process -Id $processId).ProcessName",
                "}",
                "if ($names -notcontains 'Invoke-AppDeployToolkit')",
                "{",
                "    exit 2",
                "}",
                "exit 0",
                "");
        }

        /// <summary>
        /// Gets a script that raises a console control event for its whole console and gives the launcher time to die of
        /// it, then exits with 2 if PowerShell never received the event, as an error alone exits with 1.
        /// </summary>
        /// <param name="ctrlEvent">The console control event to raise.</param>
        /// <returns>The script source.</returns>
        private static string GetConsoleControlScript(uint ctrlEvent)
        {
            return string.Join(
                Environment.NewLine,
                "$ErrorActionPreference = 'Stop'",
                "Add-Type -Namespace PSADT.Invoke.Tests -Name ConsoleControlProbe -MemberDefinition '",
                "    public delegate bool HandlerRoutine(uint ctrlType);",
                "    public static volatile bool Received;",
                "    private static readonly HandlerRoutine Record = ctrlType => Received = true;",
                "    [System.Runtime.InteropServices.DllImport(\"kernel32.dll\")] private static extern bool SetConsoleCtrlHandler(HandlerRoutine handlerRoutine, bool add);",
                "    [System.Runtime.InteropServices.DllImport(\"kernel32.dll\")] public static extern bool GenerateConsoleCtrlEvent(uint ctrlEvent, uint processGroupId);",
                "    [System.Runtime.InteropServices.DllImport(\"kernel32.dll\")] public static extern uint GetConsoleProcessList(uint[] processList, uint processCount);",
                "    public static bool Listen()",
                "    {",
                "        return SetConsoleCtrlHandler(Record, true);",
                "    }",
                "'",
                "$processIds = [System.UInt32[]]::new(64)",
                "$count = [PSADT.Invoke.Tests.ConsoleControlProbe]::GetConsoleProcessList($processIds, $processIds.Length)",
                "$launcher = foreach ($processId in $processIds[0..($count - 1)])",
                "{",
                "    Get-Process -Id $processId | Where-Object -Property ProcessName -EQ -Value 'Invoke-AppDeployToolkit'",
                "}",
                "$null = [PSADT.Invoke.Tests.ConsoleControlProbe]::Listen()",
                $"$null = [PSADT.Invoke.Tests.ConsoleControlProbe]::GenerateConsoleCtrlEvent({ctrlEvent.ToString(CultureInfo.InvariantCulture)}, 0)",
                "$null = $launcher.WaitForExit(5000)",
                "if (![PSADT.Invoke.Tests.ConsoleControlProbe]::Received)",
                "{",
                "    exit 2",
                "}",
                "exit 0",
                "");
        }

        private static void StopPing(string directoryPath)
        {
            string idPath = Path.Join(directoryPath, PingIdFileName);
            if (File.Exists(idPath))
            {
                _ = KillProcessTree(int.Parse(File.ReadAllText(idPath).Trim(), CultureInfo.InvariantCulture));
            }
        }

        private static string GetInvokerPath()
        {
            string outputPath = Path.Join(AppContext.BaseDirectory, InvokerFileName);
            if (File.Exists(outputPath))
            {
                return outputPath;
            }

            DirectoryInfo baseDirectory = new(AppContext.BaseDirectory);
            string configuration = baseDirectory.Parent?.Name ?? "Debug";
            string projectOutputPath = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "..", "..", "..", "PSADT.Invoke", "bin", configuration, "net472", InvokerFileName));
            return File.Exists(projectOutputPath)
                ? projectOutputPath
                : throw new FileNotFoundException("Unable to find the launcher executable in the test output directory.", outputPath);
        }

        private static string GetScriptPath(string directoryPath, string invocationMode)
        {
            return invocationMode.Equals(DefaultMode, StringComparison.Ordinal)
                ? Path.Join(directoryPath, "Invoke-AppDeployToolkit.ps1")
                : Path.Join(directoryPath, "Exit With Code.ps1");
        }

        private static string QuoteArgument(string argument)
        {
            return argument.IndexOfAny([' ', '\t', '\r', '\n']) == -1
                ? argument
                : "\"" + argument.Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// Starts PowerShell 7 without a console window to run a command.
        /// </summary>
        /// <param name="command">The command to run, which must not contain a double quote.</param>
        /// <param name="workingDirectory">The directory to run the command in.</param>
        /// <returns>The PowerShell 7 process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if PowerShell 7 cannot be started.</exception>
        private static Process StartPowerShellCore(string command, string workingDirectory)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "pwsh.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"{command}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory,
            };
            return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PowerShell 7.");
        }

        private static Process StartWhereProcess()
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "where.exe",
                Arguments = "pwsh.exe",
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start where.exe.");
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            private TemporaryDirectory(string directoryPath)
            {
                DirectoryPath = directoryPath;
            }

            internal string DirectoryPath { get; }

            internal static TemporaryDirectory Create()
            {
                string directoryPath = Path.Join(Path.GetTempPath(), "PSADT.Invoke.Tests", Guid.NewGuid().ToString("N"));
                _ = Directory.CreateDirectory(directoryPath);
                return new(directoryPath);
            }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(DirectoryPath, recursive: true);
                }
                catch (DirectoryNotFoundException ex)
                {
                    Trace.WriteLine(ex);
                }
                catch (IOException ex)
                {
                    Trace.WriteLine(ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Trace.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// A launcher started with its output and error streams redirected, recording every line it writes to either.
        /// </summary>
        private sealed class RedirectedInvoker : IDisposable
        {
            private readonly ConcurrentQueue<string> lines = new();

            private RedirectedInvoker(Process process)
            {
                Process = process;
                Process.OutputDataReceived += (sender, e) => Record(e.Data);
                Process.ErrorDataReceived += (sender, e) => Record(e.Data);
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();
            }

            internal Process Process { get; }

            internal IReadOnlyCollection<string> Lines => lines;

            internal static RedirectedInvoker Start(string invokerPath, string scriptPath)
            {
                ProcessStartInfo startInfo = CreateInvokerStartInfo(invokerPath, DefaultMode, scriptPath);
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                return new(Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the launcher process."));
            }

            public void Dispose()
            {
                Process.Dispose();
            }

            private void Record(string? line)
            {
                if (line is not null)
                {
                    lines.Enqueue(line);
                }
            }
        }
    }
}
