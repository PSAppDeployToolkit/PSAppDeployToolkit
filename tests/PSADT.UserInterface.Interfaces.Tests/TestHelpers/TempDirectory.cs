using System;
using System.Globalization;
using System.IO;

namespace PSADT.UserInterface.Interfaces.Tests.TestHelpers
{
    /// <summary>
    /// A scratch directory that exists for the lifetime of one test and is removed with it.
    /// </summary>
    /// <remarks>
    /// Tests in this assembly query system state but never change it. What writes here is the fixtures
    /// themselves: the image loaders under test take a path and open the file, so a test covering the
    /// file path branch has to put a real file somewhere. Putting it under here keeps that rule enforced
    /// by where the file goes rather than by the discipline of each test.
    /// <para>
    /// The names matter as well as the location. Both image loaders cache by path in a static dictionary
    /// that lives for the process and is never evicted, so a test reusing a name another test already
    /// loaded would silently assert against the earlier test's image. A directory per test, named with a
    /// GUID, makes every path unique without any test having to think about it.
    /// </para>
    /// </remarks>
    internal sealed class TempDirectory : IDisposable
    {
        /// <summary>
        /// Creates a uniquely named directory beneath the user's temporary directory.
        /// </summary>
        public TempDirectory()
        {
            Directory = System.IO.Directory.CreateDirectory(Path.Join(
                Path.GetTempPath(),
                $"PSADT.UserInterface.Interfaces.Tests_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}"));
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
