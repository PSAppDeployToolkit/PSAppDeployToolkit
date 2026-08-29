using System;
using System.Linq;
using System.Reflection;
using PSADT.SMBIOS;
using Xunit;

namespace PSADT.Tests.SMBIOS
{
    /// <summary>
    /// Contains unit tests for the structure type enumeration that identifies each SMBIOS table.
    /// </summary>
    /// <remarks>
    /// The value of every member is fixed by the SMBIOS specification, and firmware writes those numbers
    /// into the table it hands back, so a wrong one makes a reader silently parse the wrong structure.
    /// Rather than restate all forty-nine assignments, this asserts the structural properties the
    /// specification guarantees - the defined types run without gaps from zero, and the two reserved
    /// values sit at the top of the byte - and then pins the handful of members the readers in this
    /// assembly actually look for.
    /// </remarks>
    public sealed class SmbiosTypeTests
    {
        /// <summary>
        /// Verifies the members the SMBIOS readers in this assembly request by name, since a wrong value
        /// on any of these sends a reader to a different structure.
        /// </summary>
        [Fact]
        public void EnumValues_MatchSpecification()
        {
            Assert.Equal<byte>(0, (byte)SmbiosType.PlatformFirmwareInformation);
            Assert.Equal<byte>(1, (byte)SmbiosType.SystemInformation);
            Assert.Equal<byte>(2, (byte)SmbiosType.BaseboardInformation);
            Assert.Equal<byte>(3, (byte)SmbiosType.SystemEnclosure);
            Assert.Equal<byte>(4, (byte)SmbiosType.ProcessorInformation);
            Assert.Equal<byte>(126, (byte)SmbiosType.Inactive);
            Assert.Equal<byte>(127, (byte)SmbiosType.EndOfTable);
        }

        /// <summary>
        /// Verifies that the defined structure types run from zero without gaps, which is how the
        /// specification allocates them and what makes a missing member visible.
        /// </summary>
        [Fact]
        public void EnumValues_AreContiguousBelowTheReservedRange()
        {
            // Arrange
            byte[] defined = [.. DeclaredValues().Where(static v => v < 126)];
            Array.Sort(defined);

            // Assert
            Assert.Equal([.. Enumerable.Range(0, defined.Length).Select(static i => (byte)i)], defined);
        }

        /// <summary>
        /// Verifies that the two reserved values are the only members at the top of the range, so a new
        /// specification type added later cannot be given one of them by accident.
        /// </summary>
        [Fact]
        public void EnumValues_ReserveOnlyTheTopTwo()
        {
            // Arrange
            byte[] reserved = [.. DeclaredValues().Where(static v => v >= 126)];
            Array.Sort(reserved);

            // Assert
            Assert.Equal<byte[]>([126, 127], reserved);
        }

        /// <summary>
        /// Verifies that no two members share a value, which would make one of them unreachable when
        /// mapping a number read from firmware back to a name.
        /// </summary>
        [Fact]
        public void EnumValues_AreUnique()
        {
            // Arrange
            byte[] values = [.. DeclaredValues()];

            // Assert
            Assert.Equal(values.Length, values.Distinct().Count());
        }

        /// <summary>
        /// Verifies that the enumeration is a byte, since that is the width of the type field in the
        /// structure header and a wider one would misread the table.
        /// </summary>
        [Fact]
        public void Enum_IsBackedByAByte()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(SmbiosType)));
        }

        /// <summary>
        /// Verifies that the entry point type is a byte and covers the two structure table layouts plus
        /// the unknown case, which is what the version detection selects between.
        /// </summary>
        [Fact]
        public void SmbiosEntryPointType_CoversBothTableLayouts()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(SmbiosEntryPointType)));
            Assert.Equal<byte>(0, (byte)SmbiosEntryPointType.Unknown);
            Assert.Equal<byte>(1, (byte)SmbiosEntryPointType.Smbios2x);
            Assert.Equal<byte>(2, (byte)SmbiosEntryPointType.Smbios3x);
        }

        /// <summary>
        /// The value of every member declared by the structure type enumeration.
        /// </summary>
        /// <remarks>
        /// Read from the enumeration's fields rather than through <c language="csharp">Enum.GetValues</c>, which has no
        /// generic overload on every target framework this project builds for.
        /// </remarks>
        /// <returns>The declared values, in declaration order.</returns>
        private static System.Collections.Generic.IEnumerable<byte> DeclaredValues()
        {
            return typeof(SmbiosType)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(static f => (byte)(f.GetRawConstantValue() ?? (byte)0));
        }
    }
}
