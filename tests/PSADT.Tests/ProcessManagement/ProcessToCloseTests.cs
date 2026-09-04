using System;
using System.Diagnostics;
using System.Linq;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the summary of a running process that a close-applications prompt is built from.
    /// </summary>
    /// <remarks>
    /// This is the last thing between a matched process and a person being asked to close it, so what
    /// matters is that everything the prompt shows is populated: a name, a description worth reading, and
    /// a path that actually exists.
    /// </remarks>
    public sealed class ProcessToCloseTests
    {
        /// <summary>
        /// Verifies that a matched process carries over into the summary with everything the prompt needs.
        /// </summary>
        [Fact]
        public void ProcessToClose_CarriesOverTheMatchedProcess()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            RunningProcessInfo? host = RunningProcessInfo.Get([new(current.ProcessName, "A supplied description")]).FirstOrDefault(info => info.Process.Id == current.Id);
            Assert.NotNull(host);

            // Act
            ProcessToClose toClose = new(host);

            // Assert
            Assert.Equal(current.ProcessName, toClose.Name);
            Assert.Equal("A supplied description", toClose.Description);
            Assert.Equal(host.FileName.FullName, toClose.Path.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.True(toClose.Path.Exists, $"The reported path {toClose.Path.FullName} does not exist.");
        }
    }
}
