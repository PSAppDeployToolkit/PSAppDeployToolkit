using System;
using System.Globalization;
using System.IO;

namespace PSADT.UserInterface.Tests.TestHelpers
{
    /// <summary>
    /// A scratch directory that exists for the lifetime of one test and is removed with it.
    /// </summary>
    /// <remarks>
    /// Tests in this assembly query system state but never change it. The one thing here that writes at
    /// all is <see cref="DrawingUtilities.ConvertBitmapFileToIcon"/>, which takes an
    /// output path and writes to it; giving it a path under here keeps that rule enforced by where the
    /// file goes rather than by the discipline of each test. Disposal is best-effort, since a handle the
    /// code under test still holds must not turn a passing test into a failing one.
    /// </remarks>
    internal sealed class TempDirectory : IDisposable
    {
        /// <summary>
        /// Creates a uniquely named directory beneath the user's temporary directory.
        /// </summary>
        public TempDirectory()
        {
            // A GUID rather than a counter, because xunit runs test collections in parallel and this
            // has to stay unique across them without any shared state.
            Directory = System.IO.Directory.CreateDirectory(Path.Join(
                Path.GetTempPath(),
                $"PSADT.UserInterface.Tests_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}"));
        }

        /// <summary>
        /// The directory itself.
        /// </summary>
        public DirectoryInfo Directory { get; }

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
        /// Writes a file into this directory and returns its path.
        /// </summary>
        /// <param name="name">The name to give the file.</param>
        /// <param name="contents">The bytes to write.</param>
        /// <returns>The full path of the written file.</returns>
        public string WriteFile(string name, byte[] contents)
        {
            string path = GetPath(name);
            File.WriteAllBytes(path, contents);
            return path;
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
