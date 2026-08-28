using System;
using System.Collections.Generic;
using System.Linq;
using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the object attribute flags, three of which are hand-typed from the unofficial headers because
    /// CsWin32 does not surface them.
    /// </summary>
    /// <remarks>
    /// This is a flag enumeration, so the duplicate sweep in EnumAliasSweepTests excludes it. Without the
    /// assertions here nothing would notice one of the three landing on the wrong bit.
    /// </remarks>
    public sealed class OBJECT_ATTRIBUTESTests
    {
        /// <summary>
        /// Verifies that the flags are the thirteen consecutive bits from the lowest, and that the validity
        /// mask is exactly the flags Windows itself defines.
        /// </summary>
        /// <remarks>
        /// Together the two assertions pin all three hand-typed flags: distinct single bits covering an
        /// unbroken run from bit zero leaves each one only one value it can hold, and the mask then
        /// confirms which three they are, since it deliberately excludes the flags that are not valid from
        /// user mode.
        /// </remarks>
        [Fact]
        public void Values_AreConsecutiveBitsAndTheMaskExcludesTheUnofficialOnes()
        {
            // Arrange
            KeyValuePair<string, long>[] members = [.. EnumMembers.Get(typeof(OBJECT_ATTRIBUTES)).Where(static m => !string.Equals(m.Key, nameof(OBJECT_ATTRIBUTES.OBJ_VALID_ATTRIBUTES), StringComparison.Ordinal))];
            long[] unofficial =
            [
                (long)OBJECT_ATTRIBUTES.OBJ_PROTECT_CLOSE,
                (long)OBJECT_ATTRIBUTES.OBJ_AUDIT_OBJECT_CLOSE,
                (long)OBJECT_ATTRIBUTES.OBJ_NO_RIGHTS_UPGRADE,
            ];

            // Assert: thirteen distinct single bits, occupying an unbroken run from the lowest
            Assert.Equal(13, members.Length);
            foreach (KeyValuePair<string, long> member in members)
            {
                Assert.True(member.Value is not 0 && (member.Value & (member.Value - 1)) is 0, $"{member.Key} is not a single bit");
            }
            Assert.Equal(members.Length, members.Select(static m => m.Value).Distinct().Count());
            Assert.Equal((1L << members.Length) - 1, members.Aggregate(0L, static (bits, m) => bits | m.Value));

            // Assert: the mask is everything except the three the unofficial headers add
            long documented = members.Where(m => !unofficial.Contains(m.Value)).Aggregate(0L, static (mask, m) => mask | m.Value);
            Assert.Equal((long)OBJECT_ATTRIBUTES.OBJ_VALID_ATTRIBUTES, documented);
        }
    }
}
