using System;
using System.Diagnostics;
using PSADT.Foundation;
using Xunit;

namespace PSADT.Tests.Foundation
{
    /// <summary>
    /// Tests the record of which executable is hosting this library.
    /// </summary>
    /// <remarks>
    /// This is read once in a static constructor and used to decide whether the caller is one of the
    /// client executables, so getting it wrong changes how every client operation is launched. It is also
    /// the reason that constructor exists at all: the process it reads has to be disposed, which cannot be
    /// done from a field initialiser.
    /// </remarks>
    public sealed class AssemblyManagerTests
    {
        /// <summary>
        /// Verifies that the path recorded is the executable hosting the tests, and that it exists.
        /// </summary>
        [Fact]
        public void CallingProcessPath_IsTheHostingExecutable()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? expected = current.MainModule?.FileName;
            Assert.NotNull(expected);

            // Assert
            Assert.Equal(expected, AssemblyManager.CallingProcessPath.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.True(AssemblyManager.CallingProcessPath.Exists, $"The recorded path {AssemblyManager.CallingProcessPath.FullName} does not exist.");
        }
    }
}
