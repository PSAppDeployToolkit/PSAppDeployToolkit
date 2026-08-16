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
        private const int ProcessTimeoutMilliseconds = 30000;
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
            bool completed = process.WaitForExit(ProcessTimeoutMilliseconds);
            if (!completed)
            {
                process.Kill();
            }

            Assert.True(completed);
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
                File.Copy(sourceFilePath, Path.Combine(directoryPath, Path.GetFileName(sourceFilePath)));
            }
            return Path.Combine(directoryPath, InvokerFileName);
        }

        private static string GetExitScript(int exitCode)
        {
            return "exit " + exitCode.ToString(CultureInfo.InvariantCulture) + Environment.NewLine;
        }

        private static string GetInvokerPath()
        {
            string outputPath = Path.Combine(AppContext.BaseDirectory, InvokerFileName);
            if (File.Exists(outputPath))
            {
                return outputPath;
            }

            DirectoryInfo baseDirectory = new(AppContext.BaseDirectory);
            string configuration = baseDirectory.Parent?.Name ?? "Debug";
            string projectOutputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "PSADT.Invoke", "bin", configuration, "net472", InvokerFileName));
            return File.Exists(projectOutputPath)
                ? projectOutputPath
                : throw new FileNotFoundException("Unable to find the launcher executable in the test output directory.", outputPath);
        }

        private static string GetScriptPath(string directoryPath, string invocationMode)
        {
            return invocationMode.Equals(DefaultMode, StringComparison.Ordinal)
                ? Path.Combine(directoryPath, "Invoke-AppDeployToolkit.ps1")
                : Path.Combine(directoryPath, "Exit With Code.ps1");
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
                string directoryPath = Path.Combine(Path.GetTempPath(), "PSADT.Invoke.Tests", Guid.NewGuid().ToString("N"));
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
