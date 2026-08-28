using System;
using System.Diagnostics;
using System.IO;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the exception the server raises when talking to its client goes wrong.
    /// </summary>
    /// <remarks>
    /// What it adds to the one it derives from is the handle of the client it was talking to, so that a
    /// caller holding several sessions can tell which one failed. It is optional, and the overload that
    /// leaves it out is the one used by the log reader - which fails without a command in flight and so has
    /// no particular client to name. A caller reading it therefore has to expect nothing, which is what
    /// most of this asserts.
    /// </remarks>
    public sealed class ServerExceptionTests
    {
        /// <summary>
        /// Verifies that the message and the cause are both kept.
        /// </summary>
        [Fact]
        public void ServerException_CarriesTheMessageAndTheCause()
        {
            // Arrange
            IOException cause = new("the cause");

            // Act
            ServerException exception = new("a failure", cause);

            // Assert
            Assert.Equal("a failure", exception.Message);
            Assert.Same(cause, exception.InnerException);
        }

        /// <summary>
        /// Verifies that no client is named when none was given, which is the case the log reader raises.
        /// </summary>
        [Fact]
        public void ServerException_NamesNoClientWhenNoneWasGiven()
        {
            Assert.Null(new ServerException("a failure", (Exception?)null).ClientProcess);
            Assert.Null(new ServerException("a failure", new IOException("the cause")).ClientProcess);
        }

        /// <summary>
        /// Verifies that the client is named when one was given, by both of the overloads that take one.
        /// </summary>
        /// <remarks>
        /// Built around the test process itself rather than around one started for the occasion. The handle
        /// is only being carried, not used, so a real client would prove nothing that this does not - and
        /// nothing is launched, which keeps the test to reading the machine rather than changing it.
        /// </remarks>
        [Fact(Skip = "Requires the client/server executables alongside the test assembly.", SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void ServerException_NamesTheClientItWasGiven()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            ProcessHandle client = new(new ProcessLaunchInfo(Path.Join(Environment.SystemDirectory, "cmd.exe"), createNoWindow: true), current);
            IOException cause = new("the cause");

            // Act
            ServerException withoutCause = new("a failure", client);
            ServerException withCause = new("a failure", cause, client);

            // Assert
            Assert.Same(client, withoutCause.ClientProcess);
            Assert.Null(withoutCause.InnerException);
            Assert.Same(client, withCause.ClientProcess);
            Assert.Same(cause, withCause.InnerException);
        }

        /// <summary>
        /// Verifies that it is an invalid operation exception, since that is what a caller unaware of this
        /// type would be catching.
        /// </summary>
        [Fact]
        public void ServerException_IsAnInvalidOperationException()
        {
            _ = Assert.IsAssignableFrom<InvalidOperationException>(new ServerException("a failure", (Exception?)null));
        }
    }
}
