using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// A ping started for a test to look at, and killed when the test is done with it.
    /// </summary>
    /// <remarks>
    /// <see cref="StartAsync"/> returns once the ping has written its first line, which it cannot do until the loader
    /// has finished. Enumerating a process's modules any sooner can find none.
    /// </remarks>
    internal sealed class RunningPing : IDisposable
    {
        /// <summary>
        /// Takes ownership of a started ping.
        /// </summary>
        /// <param name="process">The ping's process.</param>
        private RunningPing(Process process)
        {
            Process = process;
        }

        /// <summary>
        /// The ping's process.
        /// </summary>
        public Process Process { get; }

        /// <summary>
        /// Puts the message resources a copied ping needs to write any output beside the copy.
        /// </summary>
        /// <remarks>
        /// A copy that cannot find its MUI file writes nothing, so <see cref="StartAsync"/> would wait for it to exit.
        /// Only the native ping ships one, and the 32-bit ping loads it just the same. Its version strings then come
        /// from the native ping's MUI file, so read a copy's version before calling this.
        /// </remarks>
        /// <param name="sourcePath">The ping executable that was copied.</param>
        /// <param name="destinationPath">The copy, which may be under another name.</param>
        public static void CopyMessageResources(string sourcePath, string destinationPath)
        {
            string sourceMuiName = $"{Path.GetFileName(sourcePath)}.mui";
            string destinationMuiName = $"{Path.GetFileName(destinationPath)}.mui";
            foreach (DirectoryInfo language in new DirectoryInfo(Environment.SystemDirectory).EnumerateDirectories())
            {
                FileInfo mui = new(Path.Join(language.FullName, sourceMuiName));
                if (mui.Exists)
                {
                    DirectoryInfo target = Directory.CreateDirectory(Path.Join(Path.GetDirectoryName(destinationPath), language.Name));
                    _ = mui.CopyTo(Path.Join(target.FullName, destinationMuiName));
                }
            }
        }

        /// <summary>
        /// Starts the given ping executable against the loopback address, for longer than any test runs.
        /// </summary>
        /// <param name="imagePath">The ping executable to run, which may be a copy under another name.</param>
        /// <returns>The running ping.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the ping could not be started.</exception>
        public static async Task<RunningPing> StartAsync(string imagePath)
        {
            RunningPing ping = new(Process.Start(new ProcessStartInfo(imagePath, "-n 120 127.0.0.1")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }) ?? throw new InvalidOperationException($"Failed to start [{imagePath}]."));
            try
            {
                _ = await ping.Process.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                return ping;
            }
            catch
            {
                ping.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Kills the ping and waits for it to go, so that its image can be removed afterwards.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (!ProcessUtilities.HasProcessExited(Process))
                {
                    Process.Kill();
                    _ = Process.WaitForExit(30_000);
                }
            }
            catch (InvalidOperationException)
            {
                // It exited between the check and the kill, which is the outcome wanted anyway.
            }
            finally
            {
                Process.Dispose();
            }
        }
    }
}
