using System.Linq;
using System.Security.AccessControl;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the local security authority access mask, whose four composite members are assembled by hand
    /// from standard rights and individual policy bits.
    /// </summary>
    /// <remarks>
    /// The source spells the standard rights as FileSystemRights values, which carry the same numbers.
    /// That substitution is checked here too, since a wrong one would widen or narrow every composite at
    /// once without changing anything a reader would notice.
    /// </remarks>
    public sealed class LSA_POLICY_ACCESSTests
    {
        /// <summary>
        /// The specific policy rights, as opposed to the four composites assembled from them.
        /// </summary>
        private static readonly LSA_POLICY_ACCESS[] Specific =
        [
            LSA_POLICY_ACCESS.POLICY_VIEW_LOCAL_INFORMATION,
            LSA_POLICY_ACCESS.POLICY_VIEW_AUDIT_INFORMATION,
            LSA_POLICY_ACCESS.POLICY_GET_PRIVATE_INFORMATION,
            LSA_POLICY_ACCESS.POLICY_TRUST_ADMIN,
            LSA_POLICY_ACCESS.POLICY_CREATE_ACCOUNT,
            LSA_POLICY_ACCESS.POLICY_CREATE_SECRET,
            LSA_POLICY_ACCESS.POLICY_CREATE_PRIVILEGE,
            LSA_POLICY_ACCESS.POLICY_SET_DEFAULT_QUOTA_LIMITS,
            LSA_POLICY_ACCESS.POLICY_SET_AUDIT_REQUIREMENTS,
            LSA_POLICY_ACCESS.POLICY_AUDIT_LOG_ADMIN,
            LSA_POLICY_ACCESS.POLICY_SERVER_ADMIN,
            LSA_POLICY_ACCESS.POLICY_LOOKUP_NAMES,
            LSA_POLICY_ACCESS.POLICY_NOTIFICATION,
        ];

        /// <summary>
        /// Verifies that the specific rights are distinct single bits occupying an unbroken run from the
        /// lowest, which is how the headers lay them out.
        /// </summary>
        [Fact]
        public void SpecificRights_AreConsecutiveSingleBits()
        {
            // Act
            uint combined = Specific.Aggregate(0u, static (bits, right) => bits | (uint)right);

            // Assert
            Assert.Equal(Specific.Length, Specific.Distinct().Count());
            Assert.Equal((1u << Specific.Length) - 1, combined);
        }

        /// <summary>
        /// Verifies that the standard rights the composites are built from are the numbers they stand in
        /// for: read control alone for the three generic masks, and the full required set for all access.
        /// </summary>
        [Fact]
        public void StandardRights_AreTheNumbersTheyStandInFor()
        {
            // Assert
            Assert.Equal(0x00020000u, (uint)FileSystemRights.ReadPermissions);
            Assert.Equal(0x000F0000u, (uint)(FileSystemRights.Delete | FileSystemRights.ReadPermissions | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership));
        }

        /// <summary>
        /// Verifies that the three generic masks partition the specific rights: every specific right
        /// belongs to exactly one of them, with the notification right the single deliberate exception. A
        /// right assigned to the wrong mask still compiles and still looks plausible, and this is what
        /// would catch it.
        /// </summary>
        /// <remarks>
        /// The headers put the notification right in all access alone and in none of the three generic
        /// masks, so it is excluded here rather than treated as a gap. Adding it to one of them would fail
        /// this test, which is the point.
        /// </remarks>
        [Fact]
        public void GenericMasks_PartitionTheSpecificRightsExceptNotification()
        {
            // Arrange
            const uint readControl = (uint)FileSystemRights.ReadPermissions;
            const uint read = (uint)LSA_POLICY_ACCESS.GENERIC_READ & ~readControl;
            const uint write = (uint)LSA_POLICY_ACCESS.GENERIC_WRITE & ~readControl;
            const uint execute = (uint)LSA_POLICY_ACCESS.GENERIC_EXECUTE & ~readControl;
            uint generic = Specific.Where(static right => right is not LSA_POLICY_ACCESS.POLICY_NOTIFICATION).Aggregate(0u, static (bits, right) => bits | (uint)right);

            // Assert: no right appears in more than one mask
            Assert.Equal(0u, read & write);
            Assert.Equal(0u, read & execute);
            Assert.Equal(0u, write & execute);

            // Assert: together they cover every specific right but the notification one, and nothing else
            Assert.Equal(read | write | execute, generic);

            // Assert: each mask carries read control and nothing else from the standard set
            Assert.Equal(readControl, (uint)LSA_POLICY_ACCESS.GENERIC_READ & 0xFFFF0000u);
            Assert.Equal(readControl, (uint)LSA_POLICY_ACCESS.GENERIC_WRITE & 0xFFFF0000u);
            Assert.Equal(readControl, (uint)LSA_POLICY_ACCESS.GENERIC_EXECUTE & 0xFFFF0000u);
        }

        /// <summary>
        /// Verifies that all access is the required standard rights together with every specific right.
        /// </summary>
        /// <remarks>
        /// This is the only composite carrying the notification right, which CsWin32 does not surface and
        /// the enumeration therefore declares by hand. That makes this assertion the one place a wrong
        /// value for it would show up.
        /// </remarks>
        [Fact]
        public void AllAccess_IsTheRequiredStandardRightsWithEverySpecificRight()
        {
            // Arrange
            const uint standardRightsRequired = (uint)(FileSystemRights.Delete | FileSystemRights.ReadPermissions | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership);

            // Assert
            Assert.Equal((uint)LSA_POLICY_ACCESS.POLICY_ALL_ACCESS, standardRightsRequired | Specific.Aggregate(0u, static (bits, right) => bits | (uint)right));
        }
    }
}
