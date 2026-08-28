using System;
using System.IO;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.Extensions
{
    /// <summary>
    /// Tests the Authenticode trust check on a file.
    /// </summary>
    /// <remarks>
    /// This is load-bearing rather than informational: whether the signed build of a client executable is
    /// chosen over the compatible one turns on it, and so does whether a package is treated as coming
    /// from where it claims. The check is deliberately loud for a file that is not there - a missing file
    /// is not an untrusted file, and answering false for one would let a caller carry on against
    /// something that does not exist.
    /// </remarks>
    public sealed class FileInfoExtensionsTests
    {
        /// <summary>
        /// Verifies that a file nobody signed is reported as untrusted.
        /// </summary>
        [Fact]
        public void IsAuthenticodeTrusted_ReportsAnUnsignedFileAsUntrusted()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("unsigned.exe", "this is not a signed binary"));

            // Act & Assert
            Assert.False(file.IsAuthenticodeTrusted());
        }

        /// <summary>
        /// Verifies that a file that is not there is reported rather than treated as untrusted.
        /// </summary>
        [Fact]
        public void IsAuthenticodeTrusted_ReportsAMissingFile()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => new FileInfo(temp.GetPath("absent.exe")).IsAuthenticodeTrusted());
        }

        /// <summary>
        /// Verifies that a binary Microsoft signed is reported as trusted, which is the answer the whole
        /// check exists to give.
        /// </summary>
        [Fact(Skip = "No embedded-signed binary was found to check against.", SkipUnless = nameof(TestEnvironment.HasEmbeddedSignedExecutable), SkipType = typeof(TestEnvironment))]
        public void IsAuthenticodeTrusted_ReportsASignedBinaryAsTrusted()
        {
            // Arrange
            FileInfo? signed = TestEnvironment.EmbeddedSignedExecutable;
            Assert.NotNull(signed);

            // Act & Assert
            Assert.True(signed.IsAuthenticodeTrusted());
        }

        /// <summary>
        /// Verifies that nothing at all is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void IsAuthenticodeTrusted_RefusesANullFile()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => ((FileInfo)null!).IsAuthenticodeTrusted());
        }
    }
}
