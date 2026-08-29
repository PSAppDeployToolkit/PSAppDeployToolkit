using System;
using System.Linq;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the enumeration naming the reasons a client gives up.
    /// </summary>
    /// <remarks>
    /// These are process exit codes, not an internal vocabulary. The client returns one from its entry
    /// point and the server reads it back off the exited process, asking <c language="csharp">Enum.IsDefined</c> whether the
    /// number it got is one of these before naming it - and reporting an unknown program if it is not. So
    /// what matters is the numbers rather than the names: two codes sharing one would have the server
    /// report the wrong reason, and a code the lookup does not recognise would have it report none.
    /// </remarks>
    public sealed class ClientExitCodeTests
    {
        /// <summary>
        /// Verifies that no two reasons share a number, since the number is all that survives the client
        /// exiting.
        /// </summary>
        [Fact]
        public void ClientExitCode_HasNoDuplicateValues()
        {
            ClientExitCode[] declared = EnumValues.Declared<ClientExitCode>();
            Assert.Equal(declared.Length, declared.Distinct().Count());
        }

        /// <summary>
        /// Verifies that success is zero and nothing else is, since the server treats an exit code of zero
        /// as a clean shutdown before it looks the code up at all.
        /// </summary>
        [Fact]
        public void ClientExitCode_ReservesZeroForSuccess()
        {
            Assert.Equal(0, (int)ClientExitCode.Success);
            Assert.DoesNotContain(EnumValues.Declared<ClientExitCode>().Where(static code => code is not ClientExitCode.Success), static code => (int)code is 0);
        }

        /// <summary>
        /// Verifies that every reason is recognised by the lookup the server performs on an exit code.
        /// </summary>
        /// <remarks>
        /// The server hands <c language="csharp">Enum.IsDefined</c> the exit code as an <see cref="int"/>, which is a
        /// different question from handing it a value of this type: the answer depends on the number being
        /// declared rather than on the value being well-formed. Asked the same way here so that the answer
        /// means the same thing.
        /// </remarks>
        [Fact]
        public void ClientExitCode_IsRecognisedByTheLookupTheServerUses()
        {
            foreach (ClientExitCode code in EnumValues.Declared<ClientExitCode>())
            {
                Assert.True(Enum.IsDefined(typeof(ClientExitCode), (int)code), $"The exit code [{code}] is not recognised by its own number.");
            }
        }

        /// <summary>
        /// Verifies that a number no reason was given is not recognised, since that is what has the server
        /// report an unknown program rather than name a reason it invented.
        /// </summary>
        [Fact]
        public void ClientExitCode_DoesNotRecogniseANumberItWasNotGiven()
        {
            Assert.False(Enum.IsDefined(typeof(ClientExitCode), EnumValues.Declared<ClientExitCode>().Max(static code => (int)code) + 1));
            Assert.False(Enum.IsDefined(typeof(ClientExitCode), 9));
        }
    }
}
