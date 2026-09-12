using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        private static readonly int[] ScriptExitCodes = [0, 42, 3010];

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
            if (!process.WaitForExit(ProcessTimeoutMilliseconds))
            {
                string survivors = KillProcessTree(process.Id);
                Assert.Fail($"[{invocationMode}] did not exit within {ProcessTimeoutMilliseconds}ms. Killed:{Environment.NewLine}{survivors}");
            }

            Assert.Equal(expectedExitCode, process.ExitCode);
        }

        private static Process StartInvoker(string invokerPath, string invocationMode, string scriptPath)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = invokerPath,
                Arguments = BuildProcessArguments(invocationMode, scriptPath),
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetDirectoryName(invokerPath),
            };
            return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the launcher process.");
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
        /// The launcher starts PowerShell through ShellExecute, so no job object ties the two together and
        /// <c language="csharp">Process.Kill</c> takes only the launcher. An abandoned child holds the test's temporary
        /// directory open and competes for the runner for the rest of the job. The .NET Framework has no
        /// entireProcessTree overload, so the walk is taskkill's.
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
    }
}
