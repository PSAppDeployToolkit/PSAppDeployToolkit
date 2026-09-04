using System;
using System.Globalization;
using System.IO;

namespace PSAppDeployToolkit.Tests.TestHelpers
{
    /// <summary>
    /// A scratch directory that exists for the lifetime of one test and is removed with it.
    /// </summary>
    /// <remarks>
    /// Tests here query system state but never change it. Anything that has to be written goes in one of these, so the
    /// rule is enforced by where a file lands rather than by each test's discipline. Disposal is best-effort: a file the
    /// code under test still holds open must not turn a passing test into a failing one.
    /// </remarks>
    public sealed class TempDirectory : IDisposable
    {
        /// <summary>
        /// Creates a uniquely named directory beneath the user's temporary directory.
        /// </summary>
        public TempDirectory()
        {
            // A GUID rather than a counter, so it stays unique across parallel collections with no shared state.
            Directory = System.IO.Directory.CreateDirectory(Path.Join(
                Path.GetTempPath(),
                $"PSAppDeployToolkit.Tests_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}"));
        }

        /// <summary>
        /// The directory itself.
        /// </summary>
        public DirectoryInfo Directory { get; }

        /// <summary>
        /// The full path of the directory.
        /// </summary>
        public string FullName => Directory.FullName;

        /// <summary>
        /// Combines the given name with this directory's path without creating anything.
        /// </summary>
        /// <param name="name">The file or directory name to append.</param>
        /// <returns>The combined path.</returns>
        public string GetPath(string name)
        {
            return Path.Join(Directory.FullName, name);
        }

        /// <summary>
        /// Removes the directory and everything in it.
        /// </summary>
        public void Dispose()
        {
            try
            {
                Directory.Delete(recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                // A handle the code under test still holds must not fail a test that otherwise passed.
            }
        }
    }
}
