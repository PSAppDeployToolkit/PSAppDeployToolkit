using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// Runs PowerShell in a 32-bit process with this test host's build of PSADT loaded.
    /// </summary>
    /// <remarks>
    /// The test hosts are 64-bit, so a branch only a caller running under WOW64 takes can only be reached from
    /// another process. 32-bit Windows PowerShell is that process: it loads the net472 build beside this assembly
    /// the way the module does, and it is present wherever WOW64 is.
    /// </remarks>
    internal static class Wow64PowerShell
    {
        /// <summary>
        /// How long a script is given before the process is killed.
        /// </summary>
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Runs the given script body and reports how it ended.
        /// </summary>
        /// <param name="body">PowerShell whose output is the result. An exception it throws is reported in its place,
        /// unwrapped from what PowerShell and reflection add around it.</param>
        /// <returns>How the script ended.</returns>
        /// <exception cref="InvalidOperationException">Thrown if 32-bit Windows PowerShell could not be started.</exception>
        /// <exception cref="TimeoutException">Thrown if the script did not finish within the time allowed.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "MA0136:Raw String contains an implicit end of line character", Justification = "The literal is PowerShell source, which parses either line ending, so the source file's choice cannot change what this does.")]
        public static async Task<Wow64PowerShellResult> InvokeAsync(string body)
        {
            string script = $$"""
                $ErrorActionPreference = 'Stop'
                [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
                foreach ($name in 'System.Collections.Immutable.dll', 'PSADT.Interop.dll', 'PSADT.dll')
                {
                    $null = [System.Reflection.Assembly]::UnsafeLoadFrom([System.IO.Path]::Combine({{ToLiteral(AppContext.BaseDirectory)}}, $name))
                }
                try
                {
                    $result = & {
                {{body}}
                    }
                    "OK`t$result"
                }
                catch
                {
                    $exception = $_.Exception
                    while ($exception -is [System.Management.Automation.MethodInvocationException] -or $exception -is [System.Reflection.TargetInvocationException])
                    {
                        $exception = $exception.InnerException
                    }
                    "EX`t$($exception.GetType().FullName)`t$($exception.Message -replace '\s+', ' ')"
                }
                """;
            ProcessStartInfo startInfo = new(TestEnvironment.Wow64PowerShellExecutable.FullName, $"-NoLogo -NoProfile -NonInteractive -EncodedCommand {Convert.ToBase64String(Encoding.Unicode.GetBytes(script))}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // Module paths inherited from PowerShell 7 can break Windows PowerShell, and nothing here needs a module.
            startInfo.EnvironmentVariables.Remove("PSModulePath");
            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start [{startInfo.FileName}].");

            // Both streams are drained before the wait so a script writing more than a pipe buffer holds cannot block.
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            if (!await Task.Run(() => process.WaitForExit((int)Timeout.TotalMilliseconds), TestContext.Current.CancellationToken).ConfigureAwait(false))
            {
                TryKill(process);
                throw new TimeoutException(string.Create(CultureInfo.InvariantCulture, $"32-bit Windows PowerShell did not finish within {Timeout.TotalSeconds} seconds."));
            }
            return new(process.ExitCode, await standardOutput.ConfigureAwait(false), await standardError.ConfigureAwait(false));
        }

        /// <summary>
        /// Quotes a value as a PowerShell string literal that expands nothing.
        /// </summary>
        /// <param name="value">The value to quote.</param>
        /// <returns>The value in single quotes, with any single quote in it doubled.</returns>
        private static string ToLiteral(string value)
        {
            return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
        }

        /// <summary>
        /// Kills a process that outstayed its bound, ignoring a race with its own exit.
        /// </summary>
        /// <param name="process">The process to kill.</param>
        private static void TryKill(Process process)
        {
            try
            {
                if (!ProcessUtilities.HasProcessExited(process))
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // The process exited between the check and the kill, which is the outcome wanted anyway.
            }
        }
    }
}
