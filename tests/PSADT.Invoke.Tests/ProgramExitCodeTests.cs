using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PSADT.Invoke.Tests
{
    /// <summary>
    /// Tests launcher process exit-code propagation.
    /// </summary>
    public sealed class ProgramExitCodeTests
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
        private static readonly int[] ScriptExitCodes = [0, 42, 3010, -1];

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
                    if (IsPowerShellCoreAvailable())
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
            File.WriteAllText(scriptPath, GetStartPingScript(), Encoding.UTF8);

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
        /// Verifies that PowerShell runs without a console window, hidden or otherwise.
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

        private static Process StartInvoker(string invokerPath, string invocationMode, string scriptPath)
        {
            return Process.Start(CreateInvokerStartInfo(invokerPath, invocationMode, scriptPath)) ?? throw new InvalidOperationException("Failed to start the launcher process.");
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
        /// Gets a script that leaves ping running with every inheritable handle PowerShell has, records its ID, then exits.
        /// </summary>
        /// <returns>The script source.</returns>
        private static string GetStartPingScript()
        {
            return string.Join(
                Environment.NewLine,
                "$startInfo = [System.Diagnostics.ProcessStartInfo]::new((Join-Path -Path ([System.Environment]::SystemDirectory) -ChildPath 'PING.EXE'), '-n 600 127.0.0.1')",
                "$startInfo.UseShellExecute = $false",
                "$startInfo.WorkingDirectory = [System.Environment]::SystemDirectory",
                $"[System.IO.File]::WriteAllText((Join-Path -Path $PSScriptRoot -ChildPath '{PingIdFileName}'), [System.Diagnostics.Process]::Start($startInfo).Id)",
                "exit 0",
                "");
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

        private static bool IsPowerShellCoreAvailable()
        {
            using Process process = StartWhereProcess();
            return process.WaitForExit(ProcessTimeoutMilliseconds) && process.ExitCode is 0;
        }

        private static string QuoteArgument(string argument)
        {
            return argument.IndexOfAny([' ', '\t', '\r', '\n']) == -1
                ? argument
                : "\"" + argument.Replace("\"", "\\\"") + "\"";
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
