using System;
using System.Management.Automation;
using System.Security.Principal;
using PSAppDeployToolkit.Attributes;
using Xunit;

namespace PSAppDeployToolkit.Tests.Attributes
{
    /// <summary>
    /// Tests the attribute that turns however a caller named an account into an <see cref="IdentityReference"/>.
    /// </summary>
    /// <remarks>
    /// It accepts a wide set on purpose, so that a caller can name an account the way they happen to have it - a SID, a
    /// domain-qualified name, a well-known type, a Windows identity, or the object <c>Get-LocalUser</c> hands back. The
    /// last of those is reached by reflection, since taking a dependency on the LocalAccounts module for one property
    /// would be worse.
    /// </remarks>
    public sealed class IdentityReferenceTransformationAttributeTests
    {
        /// <summary>
        /// Verifies that an identity reference passes straight through, whichever kind it is.
        /// </summary>
        [Fact]
        public void Transform_PassesAnIdentityReferenceThrough()
        {
            // Arrange
            SecurityIdentifier sid = new(WellKnownSidType.LocalSystemSid, domainSid: null);
            NTAccount account = new("BUILTIN\\Administrators");

            // Assert
            Assert.Same(sid, Transform(sid));
            Assert.Same(account, Transform(account));
        }

        /// <summary>
        /// Verifies that a well-known type becomes the matching SID.
        /// </summary>
        [Fact]
        public void Transform_TurnsAWellKnownTypeIntoASid()
        {
            Assert.Equal(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, domainSid: null),
                Transform(WellKnownSidType.BuiltinAdministratorsSid));
        }

        /// <summary>
        /// Verifies that the name of a well-known type is accepted as a string, whatever its case.
        /// </summary>
        /// <param name="name">The well-known type named as a string.</param>
        [Theory]
        [InlineData("LocalSystemSid")]
        [InlineData("localsystemsid")]
        [InlineData("LOCALSYSTEMSID")]
        public void Transform_AcceptsAWellKnownTypeNamedAsAString(string name)
        {
            Assert.Equal(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, domainSid: null), Transform(name));
        }

        /// <summary>
        /// Verifies that a SID in string form becomes a security identifier.
        /// </summary>
        [Fact]
        public void Transform_AcceptsASidInStringForm()
        {
            Assert.Equal(new SecurityIdentifier("S-1-5-18"), Transform("S-1-5-18"));
        }

        /// <summary>
        /// Verifies that a Windows identity is reduced to the account it belongs to.
        /// </summary>
        [Fact]
        public void Transform_ReducesAWindowsIdentityToItsUser()
        {
            // Arrange
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // Assert
            Assert.Equal(identity.User, Transform(identity));
        }

        /// <summary>
        /// Verifies that a Windows identity belonging to nobody is refused rather than quietly yielding nothing.
        /// </summary>
        /// <remarks>
        /// The anonymous identity has no user, which is the only way to reach this branch without a token of one's own.
        /// </remarks>
        [Fact]
        public void Transform_RefusesAWindowsIdentityWithNoUser()
        {
            // Arrange
            using WindowsIdentity anonymous = WindowsIdentity.GetAnonymous();

            // Assert
            Assert.Null(anonymous.User);
            _ = Assert.Throws<InvalidOperationException>(() => Transform(anonymous));
        }

        /// <summary>
        /// Verifies that the SID is read off an object shaped like the one the LocalAccounts module produces.
        /// </summary>
        /// <remarks>
        /// Matched by namespace and type name rather than by a real reference, so a stand-in declared in that namespace
        /// is exactly what the code under test is looking for - there is no way to distinguish it, which is the point of
        /// testing it this way.
        /// </remarks>
        [Fact]
        public void Transform_ReadsTheSidOffALocalPrincipal()
        {
            // Arrange
            SecurityIdentifier sid = new(WellKnownSidType.BuiltinUsersSid, domainSid: null);

            // Assert
            Assert.Same(sid, Transform(new Microsoft.PowerShell.Commands.LocalPrincipal(sid)));
        }

        /// <summary>
        /// Verifies that a type derived from one is accepted too.
        /// </summary>
        /// <remarks>
        /// The module's own <c>LocalUser</c> and <c>LocalGroup</c> both derive from <c>LocalPrincipal</c>, so the base
        /// type is checked separately. Without that branch, <c>Get-LocalUser | Transform</c> would be refused.
        /// </remarks>
        [Fact]
        public void Transform_ReadsTheSidOffSomethingDerivedFromALocalPrincipal()
        {
            // Arrange
            SecurityIdentifier sid = new(WellKnownSidType.BuiltinUsersSid, domainSid: null);

            // Assert
            Assert.Same(sid, Transform(new Microsoft.PowerShell.Commands.LocalUser(sid)));
        }

        /// <summary>
        /// Verifies that an identically shaped type in another namespace is refused.
        /// </summary>
        /// <remarks>
        /// The namespace is the whole of the check, so this is what stops an unrelated type that happens to carry a
        /// <c>SID</c> property being read as an account.
        /// </remarks>
        [Fact]
        public void Transform_RefusesTheSameShapeInAnotherNamespace()
        {
            _ = Assert.Throws<ArgumentException>(static () => Transform(new LookalikePrincipal(new SecurityIdentifier(WellKnownSidType.NullSid, domainSid: null))));
        }

        /// <summary>
        /// Verifies that nothing at all is refused, and named.
        /// </summary>
        [Fact]
        public void Transform_RefusesNothingAtAll()
        {
            Assert.Equal("inputData", Assert.Throws<ArgumentNullException>(static () => Transform(inputData: null)).ParamName);
        }

        /// <summary>
        /// Verifies that a blank string is refused.
        /// </summary>
        /// <param name="blank">The blank name.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Transform_RefusesABlankName(string blank)
        {
            _ = Assert.Throws<ArgumentException>(() => Transform(blank));
        }

        /// <summary>
        /// Verifies that a type it has no reading for is refused.
        /// </summary>
        [Fact]
        public void Transform_RefusesATypeItCannotRead()
        {
            _ = Assert.Throws<ArgumentException>(static () => Transform(42));
            _ = Assert.Throws<ArgumentException>(static () => Transform(Guid.Empty));
        }

        /// <summary>
        /// Verifies that a name which resolves to no account is still accepted, as an account.
        /// </summary>
        /// <remarks>
        /// Deliberate, and worth recording because it is easy to assume otherwise: an <see cref="NTAccount"/> is a name
        /// rather than a resolved account, and its constructor does not consult the machine. So the attribute accepts
        /// any non-blank name and resolution is left to whoever uses it - which is what lets a script name an account on
        /// a machine it is not currently talking to.
        /// </remarks>
        [Fact]
        public void Transform_AcceptsANameThatResolvesToNothing()
        {
            Assert.Equal(new NTAccount("no-such-account-anywhere"), Transform("no-such-account-anywhere"));
        }

        /// <summary>
        /// Verifies that a numeric string is read as an account name rather than as a well-known type.
        /// </summary>
        /// <remarks>
        /// <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> accepts the numeric form of an enumeration as
        /// well as its name, and that reading ran before the account-name one - so <c>"1"</c> came back as the Everyone
        /// SID rather than an account called "1". Leading white space and a sign are covered too, since
        /// <c>Enum.TryParse</c> tolerates both and a first-character check would not have.
        /// </remarks>
        /// <param name="digits">A name made of digits.</param>
        [Theory]
        [InlineData("1")]
        [InlineData("0")]
        [InlineData("22")]
        [InlineData(" 1")]
        [InlineData("+1")]
        public void Transform_ReadsANumericStringAsAnAccountName(string digits)
        {
            Assert.Equal(new NTAccount(digits), Transform(digits));
        }

        /// <summary>
        /// Verifies that a well-known type named properly still works after numeric forms were excluded.
        /// </summary>
        [Fact]
        public void Transform_StillAcceptsAWellKnownTypeByName()
        {
            Assert.Equal(new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null), Transform("WorldSid"));
        }

        /// <summary>
        /// Verifies that a value wrapped by PowerShell is unwrapped before being read.
        /// </summary>
        [Fact]
        public void Transform_UnwrapsAPSObject()
        {
            Assert.Equal(new SecurityIdentifier("S-1-5-18"), Transform(PSObject.AsPSObject("S-1-5-18")));
        }

        /// <summary>
        /// Runs the attribute over a value.
        /// </summary>
        /// <param name="inputData">The value to transform.</param>
        /// <returns>The identity it names.</returns>
        private static IdentityReference Transform(object? inputData)
        {
            return (IdentityReference)new IdentityReferenceTransformationAttribute().Transform(engineIntrinsics: null!, inputData);
        }

        /// <summary>
        /// A type shaped like a local principal but declared elsewhere, to confirm the namespace is what decides.
        /// </summary>
        /// <param name="sid">The account's identifier.</param>
        private sealed class LookalikePrincipal(SecurityIdentifier sid)
        {
            /// <summary>
            /// The account's identifier.
            /// </summary>
            public SecurityIdentifier SID { get; } = sid;
        }
    }
}
