using System;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the exception a client raises when it gives up.
    /// </summary>
    /// <remarks>
    /// What this type adds to the one it derives from is a single thing: it puts the exit code into the
    /// result code, so that a failure which crossed the pipe as a serialized exception still says which
    /// exit code the client would have returned. Everything asserted here is about that, and about the
    /// message and inner exception arriving alongside it.
    /// <para>
    /// That the code survives a data contract round trip - which is how it actually reaches the server,
    /// through the client's standard error - is asserted with the rest of the serialization rather than
    /// here.
    /// </para>
    /// </remarks>
    public sealed class ClientExceptionTests
    {
        /// <summary>
        /// Verifies that the exit code is carried as the result code.
        /// </summary>
        /// <remarks>
        /// Success is included deliberately. Nothing stops a caller building one with it, and the result is
        /// an exception whose result code is zero, which reads as "no error" to anything that inspects it.
        /// Asserted so that the behaviour is recorded rather than assumed.
        /// </remarks>
        /// <param name="exitCode">The exit code to carry.</param>
        [Theory]
        [InlineData(ClientExitCode.Success)]
        [InlineData(ClientExitCode.Unknown)]
        [InlineData(ClientExitCode.InvalidRequest)]
        [InlineData(ClientExitCode.EncryptionError)]
        public void ClientException_CarriesTheExitCodeAsItsResultCode(ClientExitCode exitCode)
        {
            Assert.Equal((int)exitCode, new ClientException("a failure", exitCode).HResult);
            Assert.Equal((int)exitCode, new ClientException("a failure", exitCode, new InvalidOperationException()).HResult);
        }

        /// <summary>
        /// Verifies that the message and the cause are both kept.
        /// </summary>
        [Fact]
        public void ClientException_CarriesTheMessageAndTheCause()
        {
            // Arrange
            InvalidOperationException cause = new("the cause");

            // Act
            ClientException exception = new("a failure", ClientExitCode.InvalidCommand, cause);

            // Assert
            Assert.Equal("a failure", exception.Message);
            Assert.Same(cause, exception.InnerException);
            Assert.Null(new ClientException("a failure", ClientExitCode.InvalidCommand).InnerException);
        }

        /// <summary>
        /// Verifies that it is an invalid operation exception, since that is what a caller unaware of this
        /// type would be catching.
        /// </summary>
        /// <remarks>
        /// The type is internal to the assembly pair, so a caller outside it - the deployment session, for
        /// one - only ever sees it as whatever it derives from.
        /// </remarks>
        [Fact]
        public void ClientException_IsAnInvalidOperationException()
        {
            _ = Assert.IsAssignableFrom<InvalidOperationException>(new ClientException("a failure", ClientExitCode.Unknown));
        }
    }
}
