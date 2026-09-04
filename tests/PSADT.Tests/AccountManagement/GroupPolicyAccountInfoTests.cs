using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using PSADT.AccountManagement;
using Xunit;

namespace PSADT.Tests.AccountManagement
{
    /// <summary>
    /// Tests reading the accounts that group policy has been applied to on this machine.
    /// </summary>
    /// <remarks>
    /// A machine that has never had a policy applied has nothing to report, and that is a valid answer
    /// rather than a failure - so nothing here asserts that anything is found. What is asserted is that
    /// whatever is found is coherent: each entry names an account, carries the identifier that account
    /// resolves to, and appears once.
    /// </remarks>
    public sealed class GroupPolicyAccountInfoTests
    {
        /// <summary>
        /// Verifies that the accounts read are described completely, and that the same account is not
        /// reported twice.
        /// </summary>
        [Fact]
        public void Get_DescribesEachAccountOnce()
        {
            // Act
            IReadOnlyList<GroupPolicyAccountInfo> accounts = GroupPolicyAccountInfo.Get();

            // Assert
            Assert.All(accounts, static account =>
            {
                Assert.False(string.IsNullOrWhiteSpace(account.Username.Value));
                Assert.NotNull(account.SID);
            });
            Assert.Equal(accounts.Count, accounts.Select(static account => account.SID).Distinct().Count());
        }

        /// <summary>
        /// Verifies that reading twice gives the same answer, since nothing here changes what is stored
        /// and callers ask more than once.
        /// </summary>
        [Fact]
        public void Get_IsStableBetweenReadings()
        {
            Assert.Equal(
                GroupPolicyAccountInfo.Get().Select(static account => account.SID),
                GroupPolicyAccountInfo.Get().Select(static account => account.SID));
        }

        /// <summary>
        /// Verifies that a machine account is never reported, since the accounts group policy is applied
        /// to are users rather than the computer itself.
        /// </summary>
        [Fact]
        public void Get_ReportsOnlyUserAccounts()
        {
            Assert.All(GroupPolicyAccountInfo.Get(), static account => Assert.False(account.SID.IsWellKnown(WellKnownSidType.LocalSystemSid), "The local system account was reported as a group policy account."));
        }
    }
}
