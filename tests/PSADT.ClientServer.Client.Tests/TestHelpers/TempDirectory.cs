using System;
using System.Globalization;
using System.IO;

namespace PSADT.ClientServer.Client.Tests.TestHelpers
{
    /// <summary>
    /// A scratch directory that exists for the lifetime of one test and is removed with it.
    /// </summary>
    /// <remarks>
    /// Tests in this assembly query system state but never change it. The one thing they write is a
    /// fixture: the arguments dictionary can be given as a file path, and covering that branch needs a
    /// real file. Putting it under here keeps the rule enforced by where the file goes rather than by
    /// the discipline of each test.
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
                $"PSADT.ClientServer.Client.Tests_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}"));
        }

        /// <summary>
        /// The directory itself.
        /// </summary>
        public DirectoryInfo Directory { get; }

        /// <summary>
        /// Writes a file into this directory and returns its full path.
        /// </summary>
        /// <param name="name">The file's name.</param>
        /// <param name="content">What to write.</param>
        /// <returns>The file's full path.</returns>
        public string WriteFile(string name, string content)
        {
            string path = Path.Join(Directory.FullName, name);
            File.WriteAllText(path, content);
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
                // A scratch directory that outlives the run is not worth failing a test over.
            }
        }
    }
}
