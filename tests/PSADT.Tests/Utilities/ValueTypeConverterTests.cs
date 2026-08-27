using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests the narrowing conversions PowerShell cannot express on its own.
    /// </summary>
    /// <remarks>
    /// Every method here is a one-line unchecked cast, so the conversions themselves are covered by one
    /// theory per width rather than one test per method. What earns the rest of this file is the
    /// contract with <c>Convert-ADTValueType</c>, which builds its method name by interpolating a
    /// <see cref="ValueTypeConverter.ValueTypes"/> member into <c>"To$To"</c>. Nothing in the compiler
    /// enforces that pairing, so a member added to the enumeration without a matching method fails only
    /// at runtime, in PowerShell, with a method-not-found error.
    /// </remarks>
    public sealed class ValueTypeConverterTests
    {
        /// <summary>
        /// Verifies that every enumeration member names a conversion method that exists, which is the
        /// assumption <c>Convert-ADTValueType</c> makes when it resolves <c>"To$To"</c>.
        /// </summary>
        /// <param name="memberName">The name of the enumeration member.</param>
        [Theory]
        [MemberData(nameof(ValueTypeNames))]
        public void ValueTypes_EveryMemberHasAMatchingConversionMethod(string memberName)
        {
            // Act
            MethodInfo? method = typeof(ValueTypeConverter).GetMethod(
                $"To{memberName}",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                [typeof(long)],
                modifiers: null);

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.True(method.IsPublic);
        }

        /// <summary>
        /// Verifies that every conversion method is named after an enumeration member, so the
        /// enumeration remains a complete description of what the class offers.
        /// </summary>
        [Fact]
        public void ValueTypes_CoversEveryConversionMethod()
        {
            // Arrange
            IEnumerable<string> declaredNames = DeclaredValueTypeNames();

            // Act
            IEnumerable<string> methodNames = typeof(ValueTypeConverter)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(static m => m.Name.StartsWith("To", StringComparison.Ordinal))
                .Select(static m => m.Name["To".Length..]);

            // Assert
            Assert.Empty(methodNames.Except(declaredNames, StringComparer.Ordinal));
        }

        /// <summary>
        /// Verifies that conversion to a signed 8-bit integer truncates rather than throwing, including
        /// at the boundaries where the sign bit changes meaning.
        /// </summary>
        /// <param name="input">The value to convert.</param>
        /// <param name="expected">The truncated result.</param>
        [Theory]
        [InlineData(0L, (sbyte)0)]
        [InlineData(127L, (sbyte)127)]
        [InlineData(128L, (sbyte)-128)]
        [InlineData(255L, (sbyte)-1)]
        [InlineData(256L, (sbyte)0)]
        [InlineData(-1L, (sbyte)-1)]
        [InlineData(long.MaxValue, (sbyte)-1)]
        [InlineData(long.MinValue, (sbyte)0)]
        public void ToSByte_TruncatesToTheLowestByte(long input, sbyte expected)
        {
            Assert.Equal(expected, ValueTypeConverter.ToSByte(input));
        }

        /// <summary>
        /// Verifies that conversion to an unsigned 8-bit integer truncates rather than throwing.
        /// </summary>
        /// <param name="input">The value to convert.</param>
        /// <param name="expected">The truncated result.</param>
        [Theory]
        [InlineData(0L, (byte)0)]
        [InlineData(255L, (byte)255)]
        [InlineData(256L, (byte)0)]
        [InlineData(-1L, (byte)255)]
        [InlineData(long.MaxValue, (byte)255)]
        [InlineData(long.MinValue, (byte)0)]
        public void ToByte_TruncatesToTheLowestByte(long input, byte expected)
        {
            Assert.Equal(expected, ValueTypeConverter.ToByte(input));
        }

        /// <summary>
        /// Verifies that conversion to a signed 16-bit integer truncates rather than throwing.
        /// </summary>
        /// <param name="input">The value to convert.</param>
        /// <param name="expected">The truncated result.</param>
        [Theory]
        [InlineData(0L, (short)0)]
        [InlineData(32_767L, (short)32_767)]
        [InlineData(32_768L, (short)-32_768)]
        [InlineData(65_535L, (short)-1)]
        [InlineData(65_536L, (short)0)]
        [InlineData(-1L, (short)-1)]
        [InlineData(long.MaxValue, (short)-1)]
        [InlineData(long.MinValue, (short)0)]
        public void ToShort_TruncatesToTheLowestWord(long input, short expected)
        {
            Assert.Equal(expected, ValueTypeConverter.ToShort(input));
        }

        /// <summary>
        /// Verifies that conversion to an unsigned 16-bit integer truncates rather than throwing.
        /// </summary>
        /// <param name="input">The value to convert.</param>
        /// <param name="expected">The truncated result.</param>
        [Theory]
        [InlineData(0L, (ushort)0)]
        [InlineData(65_535L, (ushort)65_535)]
        [InlineData(65_536L, (ushort)0)]
        [InlineData(-1L, (ushort)65_535)]
        [InlineData(long.MaxValue, (ushort)65_535)]
        [InlineData(long.MinValue, (ushort)0)]
        public void ToUShort_TruncatesToTheLowestWord(long input, ushort expected)
        {
            Assert.Equal(expected, ValueTypeConverter.ToUShort(input));
        }

        /// <summary>
        /// Verifies that conversion to a signed 32-bit integer truncates rather than throwing.
        /// </summary>
        /// <param name="input">The value to convert.</param>
        /// <param name="expected">The truncated result.</param>
        [Theory]
        [InlineData(0L, 0)]
        [InlineData(2_147_483_647L, 2_147_483_647)]
        [InlineData(2_147_483_648L, -2_147_483_648)]
        [InlineData(4_294_967_295L, -1)]
        [InlineData(4_294_967_296L, 0)]
        [InlineData(-1L, -1)]
        [InlineData(long.MaxValue, -1)]
        [InlineData(long.MinValue, 0)]
        public void ToInt_TruncatesToTheLowestDoubleWord(long input, int expected)
        {
            Assert.Equal(expected, ValueTypeConverter.ToInt(input));
        }

        /// <summary>
        /// Verifies that conversion to an unsigned 32-bit integer truncates rather than throwing.
        /// </summary>
        /// <param name="input">The value to convert.</param>
        /// <param name="expected">The truncated result.</param>
        [Theory]
        [InlineData(0L, 0u)]
        [InlineData(4_294_967_295L, 4_294_967_295u)]
        [InlineData(4_294_967_296L, 0u)]
        [InlineData(-1L, 4_294_967_295u)]
        [InlineData(long.MaxValue, 4_294_967_295u)]
        [InlineData(long.MinValue, 0u)]
        public void ToUInt_TruncatesToTheLowestDoubleWord(long input, uint expected)
        {
            Assert.Equal(expected, ValueTypeConverter.ToUInt(input));
        }

        /// <summary>
        /// Verifies that conversion to an unsigned 64-bit integer reinterprets the bits rather than
        /// throwing, since no truncation is possible at the same width.
        /// </summary>
        /// <param name="input">The value to convert.</param>
        /// <param name="expected">The reinterpreted result.</param>
        [Theory]
        [InlineData(0L, 0ul)]
        [InlineData(1L, 1ul)]
        [InlineData(long.MaxValue, 9_223_372_036_854_775_807ul)]
        [InlineData(-1L, 18_446_744_073_709_551_615ul)]
        [InlineData(long.MinValue, 9_223_372_036_854_775_808ul)]
        public void ToULong_ReinterpretsTheSignBit(long input, ulong expected)
        {
            Assert.Equal(expected, ValueTypeConverter.ToULong(input));
        }

        /// <summary>
        /// Verifies that the framework-named aliases agree with the primitives they duplicate, which is
        /// what lets <c>Convert-ADTValueType</c> accept either spelling.
        /// </summary>
        /// <param name="input">The value to convert through both spellings.</param>
        [Theory]
        [InlineData(0L)]
        [InlineData(1L)]
        [InlineData(-1L)]
        [InlineData(70_000L)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public void FrameworkNamedAliases_AgreeWithTheirPrimitives(long input)
        {
            Assert.Equal(ValueTypeConverter.ToShort(input), ValueTypeConverter.ToInt16(input));
            Assert.Equal(ValueTypeConverter.ToUShort(input), ValueTypeConverter.ToUInt16(input));
            Assert.Equal(ValueTypeConverter.ToInt(input), ValueTypeConverter.ToInt32(input));
            Assert.Equal(ValueTypeConverter.ToUInt(input), ValueTypeConverter.ToUInt32(input));
            Assert.Equal(ValueTypeConverter.ToULong(input), ValueTypeConverter.ToUInt64(input));
        }

        /// <summary>
        /// Verifies that the enumeration values are contiguous from zero, which is what makes them
        /// usable as an ordered PowerShell parameter set.
        /// </summary>
        [Fact]
        public void ValueTypes_ValuesAreContiguousFromZero()
        {
            // Act
            int[] values = [.. typeof(ValueTypeConverter.ValueTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(static f => (int)(f.GetRawConstantValue() ?? 0))];

            // Assert
            Assert.Equal([.. Enumerable.Range(0, values.Length)], values);
        }

        /// <summary>
        /// The names of every member of the conversion enumeration.
        /// </summary>
        public static TheoryData<string> ValueTypeNames
        {
            get
            {
                TheoryData<string> data = [];
                foreach (string name in DeclaredValueTypeNames())
                {
                    data.Add(name);
                }
                return data;
            }
        }
        /// <summary>
        /// The names of the members declared by the conversion enumeration, read from its fields so the
        /// same code works on both target frameworks.
        /// </summary>
        /// <returns>The declared member names, in declaration order.</returns>
        private static IEnumerable<string> DeclaredValueTypeNames()
        {
            return typeof(ValueTypeConverter.ValueTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(static f => f.Name);
        }
    }
}
