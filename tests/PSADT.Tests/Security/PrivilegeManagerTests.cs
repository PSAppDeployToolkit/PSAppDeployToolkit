using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PSADT.Interop;
using PSADT.Security;
using PSADT.Tests.TestHelpers;
using Windows.Win32.Security;
using Xunit;

namespace PSADT.Tests.Security
{
    /// <summary>
    /// Tests the privilege queries against the token this process is running with.
    /// </summary>
    /// <remarks>
    /// Nothing here touches another account's token. Where a privilege is adjusted it is adjusted on this
    /// process's own token, which is discarded when the test host exits, so the machine is unchanged
    /// either way.
    /// <para>
    /// Two privileges anchor the assertions, chosen so that they hold whether or not the run is elevated:
    /// <c language="csharp">SeChangeNotifyPrivilege</c>, which every account holds and which is enabled by default, and
    /// <c language="csharp">SeCreateTokenPrivilege</c>, which is granted to the local system account alone - not to
    /// administrators - and so is absent from any token these tests will see.
    /// </para>
    /// </remarks>
    public sealed class PrivilegeManagerTests
    {
        /// <summary>
        /// Verifies that the caller's privileges are readable and that every one of them maps onto the
        /// enumeration, which is what would break first if Windows added a privilege.
        /// </summary>
        /// <remarks>
        /// The names are read off the token as strings and mapped onto the enumeration by name, so a
        /// privilege the enumeration does not know about would make the whole call throw rather than skip
        /// the one it could not place.
        /// </remarks>
        [Fact]
        public void GetPrivileges_ReadsEveryPrivilegeOnTheToken()
        {
            // Act
            ReadOnlyCollection<SE_PRIVILEGE> privileges = PrivilegeManager.GetPrivileges();

            // Assert
            Assert.NotEmpty(privileges);
            Assert.All(privileges, static privilege => Assert.Contains(privilege, EnumValues.Declared<SE_PRIVILEGE>()));
        }

        /// <summary>
        /// Verifies that filtering by attribute narrows the answer rather than widening it, so a caller
        /// asking only for the enabled privileges cannot be handed a disabled one.
        /// </summary>
        [Fact]
        public void GetPrivileges_FilteringByAttributeNarrowsTheAnswer()
        {
            // Act
            ReadOnlyCollection<SE_PRIVILEGE> all = PrivilegeManager.GetPrivileges();
            ReadOnlyCollection<SE_PRIVILEGE> enabled = PrivilegeManager.GetPrivileges(TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED);

            // Assert
            Assert.All(enabled, privilege => Assert.Contains(privilege, all));
            Assert.True(enabled.Count <= all.Count, "Filtering by attribute reported more privileges than were held.");
        }

        /// <summary>
        /// Verifies that the privilege every account holds is reported as held, which is the one assertion
        /// about privileges that is true for any caller on any machine.
        /// </summary>
        [Fact]
        public void HasPrivilege_ReportsThePrivilegeEveryAccountHolds()
        {
            Assert.True(PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeChangeNotifyPrivilege));
        }

        /// <summary>
        /// Verifies that a privilege no ordinary account is granted is reported as not held.
        /// </summary>
        [Fact(Skip = "The local system account holds this privilege.", SkipWhen = nameof(TestEnvironment.IsLocalSystem), SkipType = typeof(TestEnvironment))]
        public void HasPrivilege_ReportsAPrivilegeTheCallerDoesNotHold()
        {
            Assert.False(PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeCreateTokenPrivilege));
        }

        /// <summary>
        /// Verifies that the set of privileges reported as held matches the set the full listing contains,
        /// so the two ways of asking cannot drift apart.
        /// </summary>
        [Fact]
        public void HasPrivilege_AgreesWithTheFullListing()
        {
            // Arrange
            IReadOnlyList<SE_PRIVILEGE> privileges = PrivilegeManager.GetPrivileges();

            // Act & Assert
            Assert.All(privileges, static privilege => Assert.True(PrivilegeManager.HasPrivilege(privilege), $"{privilege} was listed but reported as not held."));
        }

        /// <summary>
        /// Verifies that a privilege reported as enabled is one the caller holds, since a privilege cannot
        /// be enabled without being present.
        /// </summary>
        [Fact]
        public void IsPrivilegeEnabled_ImpliesThePrivilegeIsHeld()
        {
            // Act & Assert
            Assert.All(
                PrivilegeManager.GetPrivileges().Where(static privilege => PrivilegeManager.IsPrivilegeEnabled(privilege)),
                static privilege => Assert.True(PrivilegeManager.HasPrivilege(privilege), $"{privilege} was reported enabled but not held."));
        }

        /// <summary>
        /// Verifies that the privileges reported as enabled are exactly those the attribute-filtered
        /// listing contains, so a caller can use either and get the same answer.
        /// </summary>
        [Fact]
        public void IsPrivilegeEnabled_AgreesWithTheFilteredListing()
        {
            // Arrange
            ReadOnlyCollection<SE_PRIVILEGE> enabled = PrivilegeManager.GetPrivileges(TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED);

            // Act & Assert
            Assert.All(enabled, static privilege => Assert.True(PrivilegeManager.IsPrivilegeEnabled(privilege), $"{privilege} was listed as enabled but reported as disabled."));
        }

        /// <summary>
        /// Verifies that the privilege every account holds is enabled, and that asking for it to be
        /// enabled when it already is changes nothing and reports no error.
        /// </summary>
        [Fact]
        public void EnablePrivilegeIfDisabled_IsHarmlessForAnEnabledPrivilege()
        {
            // Arrange: this one is enabled on every token by default
            Assert.True(PrivilegeManager.IsPrivilegeEnabled(SE_PRIVILEGE.SeChangeNotifyPrivilege));

            // Act & Assert
            Assert.Null(Record.Exception(static () => PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeChangeNotifyPrivilege)));
            Assert.True(PrivilegeManager.IsPrivilegeEnabled(SE_PRIVILEGE.SeChangeNotifyPrivilege));
        }

        /// <summary>
        /// Verifies that enabling a privilege the caller already holds leaves it enabled, which is the
        /// path every caller that needs a privilege takes.
        /// </summary>
        [Fact]
        public void EnablePrivilege_LeavesAHeldPrivilegeEnabled()
        {
            // Act
            PrivilegeManager.EnablePrivilege(SE_PRIVILEGE.SeChangeNotifyPrivilege);

            // Assert
            Assert.True(PrivilegeManager.IsPrivilegeEnabled(SE_PRIVILEGE.SeChangeNotifyPrivilege));
        }

        /// <summary>
        /// Verifies that enabling a privilege the caller does not hold is refused, rather than appearing
        /// to succeed and leaving the caller believing it has a right it does not.
        /// </summary>
        /// <remarks>
        /// This is the failure worth being loud about: <c language="csharp">AdjustTokenPrivileges</c> reports success even
        /// when it could not enable everything it was asked to, so a caller that trusted the return value
        /// would proceed without the right it thinks it has.
        /// </remarks>
        [Fact(Skip = "The local system account holds this privilege.", SkipWhen = nameof(TestEnvironment.IsLocalSystem), SkipType = typeof(TestEnvironment))]
        public void EnablePrivilege_RefusesAPrivilegeTheCallerDoesNotHold()
        {
            _ = Assert.Throws<UnauthorizedAccessException>(static () => PrivilegeManager.EnablePrivilege(SE_PRIVILEGE.SeCreateTokenPrivilege));
        }

        /// <summary>
        /// Verifies that a privilege the caller does not hold is refused by the conditional overload too,
        /// rather than being treated as already satisfied.
        /// </summary>
        [Fact(Skip = "The local system account holds this privilege.", SkipWhen = nameof(TestEnvironment.IsLocalSystem), SkipType = typeof(TestEnvironment))]
        public void EnablePrivilegeIfDisabled_RefusesAPrivilegeTheCallerDoesNotHold()
        {
            _ = Assert.Throws<UnauthorizedAccessException>(static () => PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeCreateTokenPrivilege));
        }
    }
}
