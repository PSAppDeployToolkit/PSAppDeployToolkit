using System.Collections.Generic;
using System.Globalization;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the bitfield a deployment session keeps its settings in.
    /// </summary>
    /// <remarks>
    /// Nineteen flags declared as shift expressions, read back with <c language="csharp">HasFlag</c>. A duplicated or skipped shift is the
    /// mistake this catches, and it is one nothing else would: two flags sharing a bit would make setting either appear
    /// to set both, so a session marked silent would also read as requiring administrator rights. The compiler has
    /// nothing to say about it and no other test would notice.
    /// </remarks>
    public sealed class DeploymentSettingsTests
    {
        /// <summary>
        /// Verifies that nothing set is nothing set.
        /// </summary>
        [Fact]
        public void DeploymentSettings_HasAnEmptyValueOfZero()
        {
            Assert.Equal(0u, (uint)DeploymentSettings.None);
        }

        /// <summary>
        /// Verifies that every flag but the empty one occupies exactly one bit, and no two share it.
        /// </summary>
        [Fact]
        public void DeploymentSettings_GivesEveryFlagABitOfItsOwn()
        {
            HashSet<uint> seen = [];
            foreach (DeploymentSettings setting in EnumValues.Declared<DeploymentSettings>())
            {
                uint value = (uint)setting;
                if (value is 0)
                {
                    continue;
                }
                Assert.True((value & (value - 1)) is 0, $"{setting} is 0x{value.ToString("X", CultureInfo.InvariantCulture)}, which is not a single bit.");
                Assert.True(seen.Add(value), $"{setting} shares bit 0x{value.ToString("X", CultureInfo.InvariantCulture)} with another flag.");
            }
        }

        /// <summary>
        /// Verifies that the flags together cover an unbroken run of bits from the lowest.
        /// </summary>
        /// <remarks>
        /// A gap is harmless in itself, but it is the signature of a flag removed without the ones above it being
        /// renumbered. The bitfield is never persisted, so there is no reason to leave one.
        /// </remarks>
        [Fact]
        public void DeploymentSettings_LeavesNoGapsInTheBitsItUses()
        {
            uint combined = 0;
            foreach (DeploymentSettings setting in EnumValues.Declared<DeploymentSettings>())
            {
                combined |= (uint)setting;
            }
            Assert.True(((combined + 1) & combined) is 0, $"the flags cover 0x{combined.ToString("X", CultureInfo.InvariantCulture)}, which has a gap.");
        }
    }
}
