using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the firmware table identifiers, which are hand-typed FourCC codes with one deliberate
    /// exception. The member name is a complete oracle for the number, so nothing here has to hard-code a
    /// table of expected values.
    /// </summary>
    public sealed class FIRMWARE_TABLE_IDTests
    {
        /// <summary>
        /// Verifies that every ACPI table identifier spells its own member name. The values are FourCC
        /// codes stored little-endian, so the name is a complete oracle for the number and a transposed
        /// hex digit cannot survive.
        /// </summary>
        [Fact]
        public void AcpiMembers_DecodeToTheirOwnName()
        {
            // Arrange
            const string prefix = "ACPI_";
            KeyValuePair<string, long>[] members = [.. EnumMembers.Get(typeof(FIRMWARE_TABLE_ID)).Where(static m => m.Key.StartsWith("ACPI_", StringComparison.Ordinal))];

            // Assert
            Assert.Equal(23, members.Length);
            foreach (KeyValuePair<string, long> member in members)
            {
                byte[] bytes = new byte[4];
                BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)member.Value);
                Assert.Equal(member.Key[prefix.Length..], Encoding.ASCII.GetString(bytes));
            }
        }

        /// <summary>
        /// Verifies that the SMBIOS member is zero. It is the one member that is not a FourCC: the RSMB
        /// provider takes a table identifier of zero, so the value is provider-relative rather than a
        /// packed signature, and the test above deliberately exempts it.
        /// </summary>
        /// <remarks>
        /// The exemption is checked against the provider itself in NativeMethodsTests, which asks RSMB for
        /// table zero and gets a table back.
        /// </remarks>
        [Fact]
        public void Smbios_IsTheProviderRelativeZero()
        {
            // Assert
            Assert.Equal(0u, (uint)FIRMWARE_TABLE_ID.SMBIOS);
        }
    }
}
