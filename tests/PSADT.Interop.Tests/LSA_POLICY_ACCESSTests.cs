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
        /// belongs to exactly one of them, and none is left out. A right assigned to the wrong mask still
        /// compiles and still looks plausible, and this is what would catch it.
        /// </summary>
        [Fact]
        public void GenericMasks_PartitionTheSpecificRights()
        {
            // Arrange
            const uint readControl = (uint)FileSystemRights.ReadPermissions;
            const uint read = (uint)LSA_POLICY_ACCESS.GENERIC_READ & ~readControl;
            const uint write = (uint)LSA_POLICY_ACCESS.GENERIC_WRITE & ~readControl;
            const uint execute = (uint)LSA_POLICY_ACCESS.GENERIC_EXECUTE & ~readControl;

            // Assert: no right appears in more than one mask
            Assert.Equal(0u, read & write);
            Assert.Equal(0u, read & execute);
            Assert.Equal(0u, write & execute);

            // Assert: together they cover every specific right and nothing else
            Assert.Equal(read | write | execute, Specific.Aggregate(0u, static (bits, right) => bits | (uint)right));

            // Assert: each mask carries read control and nothing else from the standard set
            Assert.Equal(readControl, (uint)LSA_POLICY_ACCESS.GENERIC_READ & 0xFFFF0000u);
            Assert.Equal(readControl, (uint)LSA_POLICY_ACCESS.GENERIC_WRITE & 0xFFFF0000u);
            Assert.Equal(readControl, (uint)LSA_POLICY_ACCESS.GENERIC_EXECUTE & 0xFFFF0000u);
        }

        /// <summary>
        /// Verifies that all access is the required standard rights together with every specific right.
        /// </summary>
        /// <remarks>
        /// The Windows headers also fold POLICY_NOTIFICATION into this mask. CsWin32 does not surface that
        /// constant and this enumeration does not declare it, so the mask here is narrower by that one bit.
        /// Nothing in this repository asks for it, and asking for less access than the headers define is
        /// safe; this test records the difference so it stays deliberate.
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
