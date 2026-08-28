using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.Security;
using Windows.Win32.Security;
using Xunit;

namespace PSADT.Tests.Security
{
    /// <summary>
    /// Tests reading facts off an access token.
    /// </summary>
    /// <remarks>
    /// The subject throughout is this process's own token, which is the one token the tests are certain to
    /// be able to open and the one whose contents can be checked against another source: the framework
    /// knows the identity, and the process knows its session.
    /// </remarks>
    public sealed class TokenUtilitiesTests
    {
        /// <summary>
        /// Verifies that the owner read from the current token is the identity the process is running as.
        /// </summary>
        [Fact]
        public void GetTokenSid_ReportsTheCurrentIdentity()
        {
            // Arrange
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY);

            // Act & Assert
            Assert.Equal(identity.User, TokenUtilities.GetTokenSid(token));
        }

        /// <summary>
        /// Verifies that the session read from the current token is the session the process is in.
        /// </summary>
        [Fact]
        public void GetTokenSessionId_ReportsTheCurrentSession()
        {
            // Arrange
            using System.Diagnostics.Process current = System.Diagnostics.Process.GetCurrentProcess();
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY);

            // Act & Assert
            Assert.Equal((uint)current.SessionId, TokenUtilities.GetTokenSessionId(token));
        }

        /// <summary>
        /// Verifies that whether the current token is administrative agrees with what the framework says
        /// about the same identity.
        /// </summary>
        /// <remarks>
        /// Both sides have to agree in both directions for this to mean anything: an unelevated member of
        /// the administrators group is not administrative, because the group is present but denied, and
        /// the check has to see that rather than merely seeing the group.
        /// </remarks>
        [Fact]
        public void IsTokenAdministrative_AgreesWithTheFramework()
        {
            // Arrange
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            bool expected = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE);

            // Act & Assert
            Assert.Equal(expected, TokenUtilities.IsTokenAdministrative(token));
        }

        /// <summary>
        /// Verifies that a token duplicated from the current one reports the same facts as the original,
        /// since duplication is how every token this library hands out is produced.
        /// </summary>
        [Fact]
        public void GetTokenSid_SurvivesDuplication()
        {
            // Arrange
            using SafeFileHandle token = TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE);

            // Act
            using SafeFileHandle duplicate = TokenManager.GetPrimaryToken(token);

            // Assert
            Assert.Equal(TokenUtilities.GetTokenSid(token), TokenUtilities.GetTokenSid(duplicate));
            Assert.Equal(TokenUtilities.GetTokenSessionId(token), TokenUtilities.GetTokenSessionId(duplicate));
        }
    }
}
