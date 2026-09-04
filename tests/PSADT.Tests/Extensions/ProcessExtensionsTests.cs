using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using PSADT.FileSystem;
using Xunit;

namespace PSADT.Tests.Extensions
{
    /// <summary>
    /// Tests resolving the image a process is running.
    /// </summary>
    /// <remarks>
    /// The image path is read in native form and translated back through a table of device names, which
    /// is why this exists rather than the framework's own module reader being used: the native form is
    /// readable for a process whose modules are not, and it is the only form available for a process of
    /// another bitness.
    /// <para>
    /// The subject is the test host, whose image the framework can also name - so the two are compared
    /// against each other.
    /// </para>
    /// </remarks>
    public sealed class ProcessExtensionsTests
    {
        /// <summary>
        /// Verifies that the image resolved for the test host is the one the framework reports for it.
        /// </summary>
        [Fact]
        public void GetFilePath_ResolvesTheTestHostsImage()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? expected = current.MainModule?.FileName;
            Assert.NotNull(expected);

            // Act
            FileInfo resolved = current.GetFilePath();

            // Assert
            Assert.Equal(expected, resolved.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.True(resolved.Exists, $"The resolved image {resolved.FullName} does not exist.");
        }

        /// <summary>
        /// Verifies that a lookup table supplied by the caller gives the same answer as letting one be
        /// built, which is what lets an enumeration build it once and reuse it.
        /// </summary>
        [Fact]
        public void GetFilePath_UsesALookupTableItIsGiven()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            ReadOnlyDictionary<string, string> lookupTable = FileSystemUtilities.MakeNtPathLookupTable();

            // Act & Assert
            Assert.Equal(current.GetFilePath().FullName, current.GetFilePath(lookupTable).FullName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that nothing at all is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void GetFilePath_RefusesANullProcess()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => ((Process)null!).GetFilePath());
        }
    }
}
