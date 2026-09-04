using System;
using System.Linq;
using System.Reflection;
using PSADT.ConfigMgr;
using Xunit;

namespace PSADT.Tests.ConfigMgr
{
    /// <summary>
    /// Contains unit tests for the Configuration Manager schedule identifiers.
    /// </summary>
    /// <remarks>
    /// Each member's value is the identifier the Configuration Manager client expects, packed as a group
    /// in the high byte and an action in the low byte. The client silently ignores an unrecognised
    /// identifier rather than reporting one, so a wrong value here produces a schedule trigger that
    /// appears to succeed and does nothing. The declarations are written as shifted expressions, which
    /// makes a mistyped shift easy to miss on review, so the packing is asserted rather than assumed.
    /// </remarks>
    public sealed class TriggerScheduleIdTests
    {
        /// <summary>
        /// Verifies the identifiers a deployment is most likely to trigger, against the values documented
        /// for the Configuration Manager client.
        /// </summary>
        [Fact]
        public void EnumValues_MatchTheDocumentedIdentifiers()
        {
            Assert.Equal<ushort>(0x0001, (ushort)TriggerScheduleId.HardwareInventory);
            Assert.Equal<ushort>(0x0002, (ushort)TriggerScheduleId.SoftwareInventory);
            Assert.Equal<ushort>(0x0003, (ushort)TriggerScheduleId.HeartbeatDiscovery);
            Assert.Equal<ushort>(0x0021, (ushort)TriggerScheduleId.RequestMachinePolicy);
            Assert.Equal<ushort>(0x0022, (ushort)TriggerScheduleId.EvaluateMachinePolicy);
            Assert.Equal<ushort>(0x0031, (ushort)TriggerScheduleId.SoftwareMeteringReport);
            Assert.Equal<ushort>(0x0032, (ushort)TriggerScheduleId.SourceUpdate);
            Assert.Equal<ushort>(0x0101, (ushort)TriggerScheduleId.HardwareInventoryCollectionCycle);
            Assert.Equal<ushort>(0x0113, (ushort)TriggerScheduleId.SoftwareUpdatesScan);
        }

        /// <summary>
        /// Verifies that no two identifiers collide, which would make one of them unreachable by name.
        /// </summary>
        [Fact]
        public void EnumValues_AreUnique()
        {
            // Arrange
            ushort[] values = [.. DeclaredValues()];

            // Assert
            Assert.Equal(values.Length, values.Distinct().Count());
        }

        /// <summary>
        /// Verifies that every identifier packs into two bytes with a non-zero action, since the client
        /// reads the low byte as the action to perform and zero names none.
        /// </summary>
        [Fact]
        public void EnumValues_PackAGroupAndANonZeroAction()
        {
            Assert.All(DeclaredValues(), static value =>
            {
                Assert.NotEqual(0, value & 0xFF);
                Assert.InRange((value >> 8) & 0xFF, 0, 0xFF);
            });
        }

        /// <summary>
        /// Verifies that the enumeration is a 16-bit unsigned integer, which is the width the client's
        /// interface declares and what makes the two-byte packing meaningful.
        /// </summary>
        [Fact]
        public void Enum_IsBackedByAUInt16()
        {
            Assert.Equal(typeof(ushort), Enum.GetUnderlyingType(typeof(TriggerScheduleId)));
        }

        /// <summary>
        /// Verifies that no identifier is zero, which the client would treat as no schedule at all.
        /// </summary>
        [Fact]
        public void EnumValues_AreNeverZero()
        {
            Assert.DoesNotContain<ushort>(0, DeclaredValues());
        }

        /// <summary>
        /// The value of every declared identifier.
        /// </summary>
        /// <remarks>
        /// Read from the enumeration's fields rather than through <c language="csharp">Enum.GetValues</c>, which has no
        /// generic overload on every target framework this project builds for.
        /// </remarks>
        /// <returns>The declared values, in declaration order.</returns>
        private static System.Collections.Generic.IEnumerable<ushort> DeclaredValues()
        {
            return typeof(TriggerScheduleId)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(static f => (ushort)(f.GetRawConstantValue() ?? (ushort)0));
        }
    }
}
