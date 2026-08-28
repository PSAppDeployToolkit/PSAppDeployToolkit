using System;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using PSADT.AccountManagement;
using Xunit;

namespace PSADT.Tests.AccountManagement
{
    /// <summary>
    /// Tests the user profile record's validation.
    /// </summary>
    /// <remarks>
    /// The type assigns its members and derives nothing, so only the guards are worth asserting. They
    /// matter because a profile is enumerated from the registry, where an account can be present with an
    /// unresolvable name, and a record that accepted one would carry an empty account through to whatever
    /// tried to act on it. Nothing here asserts that a member holds what it was handed.
    /// </remarks>
    public sealed class UserProfileInfoTests
    {
        /// <summary>
        /// Verifies that a profile with the required members is accepted and leaves the optional
        /// directories unset.
        /// </summary>
        [Fact]
        public void Constructor_AcceptsTheRequiredMembersAlone()
        {
            // Act
            UserProfileInfo profile = new(new NTAccount(@"TESTHOST\user"), WellKnownSid, new DirectoryInfo(@"C:\Users\user"));

            // Assert
            Assert.Null(profile.AppDataPath);
            Assert.Null(profile.LocalAppDataPath);
            Assert.Null(profile.DesktopPath);
            Assert.Null(profile.DocumentsPath);
            Assert.Null(profile.StartMenuPath);
            Assert.Null(profile.TempPath);
            Assert.Null(profile.OneDrivePath);
            Assert.Null(profile.OneDriveCommercialPath);
            Assert.Null(profile.UserLocale);
        }

        /// <summary>
        /// Verifies that an account with no resolvable name is rejected, which is the case a profile
        /// enumeration hits for a deleted account whose profile directory survives.
        /// </summary>
        [Fact]
        public void Constructor_RejectsAnAccountWithNoName()
        {
            _ = Assert.Throws<ArgumentException>(static () => new UserProfileInfo(new NTAccount(string.Empty), WellKnownSid, new DirectoryInfo(@"C:\Users\user")));
        }

        /// <summary>
        /// Verifies that a null account is rejected as absent.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RejectsANullAccount()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new UserProfileInfo(null!, WellKnownSid, new DirectoryInfo(@"C:\Users\user")));
        }

        /// <summary>
        /// Verifies that a null security identifier is rejected, since the identifier is what a profile is
        /// keyed by rather than the name.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RejectsANullSecurityIdentifier()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new UserProfileInfo(new NTAccount(@"TESTHOST\user"), null!, new DirectoryInfo(@"C:\Users\user")));
        }

        /// <summary>
        /// Verifies that a null profile path is rejected, since the profile directory is the one thing a
        /// caller always needs.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RejectsANullProfilePath()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new UserProfileInfo(new NTAccount(@"TESTHOST\user"), WellKnownSid, null!));
        }

        /// <summary>
        /// Verifies that two profiles describing the same account are equal, since profiles are collected
        /// into lists that are compared and deduplicated as a whole.
        /// </summary>
        /// <remarks>
        /// Each is built from its own <see cref="DirectoryInfo"/> naming the same directory rather than
        /// from one shared between them, which is the whole point: that type does not override equality,
        /// so a profile holding one directly compared by reference and two descriptions of one account
        /// never matched. The paths are recorded and rebuilt on read instead.
        /// </remarks>
        [Fact]
        public void Equality_IsByValue()
        {
            // Arrange
            NTAccount account = new(@"TESTHOST\user");

            // Act: separate objects throughout, naming the same account and the same directories
            UserProfileInfo left = new(account, WellKnownSid, new(@"C:\Users\user"), new(@"C:\Users\user\AppData\Roaming"), userLocale: CultureInfo.InvariantCulture);
            UserProfileInfo right = new(account, WellKnownSid, new(@"C:\Users\user"), new(@"C:\Users\user\AppData\Roaming"), userLocale: CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());

            // Assert: and a profile naming a different directory is not equal to either
            Assert.NotEqual(left, new UserProfileInfo(account, WellKnownSid, new(@"C:\Users\other"), userLocale: CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// A security identifier that exists on every machine, so the tests need no account of their own.
        /// </summary>
        private static SecurityIdentifier WellKnownSid { get; } = new(WellKnownSidType.LocalSystemSid, domainSid: null);
    }
}
