using System;
using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the TypedConstant base class, which backs the strongly-typed Win32 constant families such
    /// as RESOURCE_TYPE and MSI_PERSISTENCE_MODE. Everything here is pure value and string logic.
    /// </summary>
    /// <remarks>
    /// The tests drive a local subclass rather than the production constant families, because those
    /// expose only a fixed set of instances whose values are all small and non-negative. The interesting
    /// behaviour lives at the boundaries: negative values, values too wide for the requested type, and
    /// the numeric cases of the object equality overload.
    /// </remarks>
    public sealed class TypedConstantTests
    {
        /// <summary>
        /// Verifies that every conversion returns the stored value when it fits the target type.
        /// </summary>
        [Fact]
        public void Conversions_ReturnStoredValueWhenRepresentable()
        {
            // Arrange
            TestConstant constant = new(42, "Answer");

            // Assert
            Assert.Equal((sbyte)42, constant.ToSByte());
            Assert.Equal((byte)42, constant.ToByte());
            Assert.Equal((short)42, constant.ToInt16());
            Assert.Equal((ushort)42, constant.ToUInt16());
            Assert.Equal(42, constant.ToInt32());
            Assert.Equal(42u, constant.ToUInt32());
            Assert.Equal(42L, constant.ToInt64());
            Assert.Equal(42UL, constant.ToUInt64());
            Assert.Equal(42, constant.ToIntPtr());
        }

        /// <summary>
        /// Verifies that the signed conversions preserve a negative value rather than reinterpreting it.
        /// </summary>
        [Fact]
        public void Conversions_PreserveNegativeValuesForSignedTargets()
        {
            // Arrange
            TestConstant constant = new(-1, "MinusOne");

            // Assert
            Assert.Equal((sbyte)-1, constant.ToSByte());
            Assert.Equal((short)-1, constant.ToInt16());
            Assert.Equal(-1, constant.ToInt32());
            Assert.Equal(-1L, constant.ToInt64());
            Assert.Equal(-1, constant.ToIntPtr());
        }

        /// <summary>
        /// Verifies that a value which does not fit the requested type is rejected rather than silently
        /// truncated or reinterpreted.
        /// </summary>
        /// <remarks>
        /// This is a consequence of the repository building with CheckForOverflowUnderflow: the casts in
        /// these conversions are checked. It is worth pinning because the documented summaries describe
        /// them only as "the X representation of this constant's value", which reads like a truncating
        /// cast, and because a constant built from a real string pointer will not fit the narrower
        /// targets on a 64-bit process.
        /// </remarks>
        [Fact]
        public void Conversions_ThrowWhenValueDoesNotFitTarget()
        {
            // Arrange
            TestConstant tooWide = new(300, "TooWide");
            TestConstant negative = new(-1, "Negative");

            // Assert
            _ = Assert.Throws<OverflowException>(() => tooWide.ToSByte());
            _ = Assert.Throws<OverflowException>(() => tooWide.ToByte());
            _ = Assert.Throws<OverflowException>(() => negative.ToByte());
            _ = Assert.Throws<OverflowException>(() => negative.ToUInt16());
            _ = Assert.Throws<OverflowException>(() => negative.ToUInt32());
            _ = Assert.Throws<OverflowException>(() => negative.ToUInt64());
        }

        /// <summary>
        /// Verifies that a caller cannot opt out of the overflow check, and shows the one way to get
        /// truncating behaviour if that is what is wanted.
        /// </summary>
        /// <remarks>
        /// checked and unchecked are lexical: they govern the conversions written inside their own
        /// parentheses and do not reach into a method invoked from that expression. The cast lives in
        /// ToSByte's body, compiled in PSADT.Interop where CheckForOverflowUnderflow is on, so the
        /// context is fixed by the library rather than the caller. Writing the conversion at the call
        /// site instead puts it in the caller's context, where unchecked does apply.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2333:Redundant modifiers should be removed", Justification = "The redundancy is the point: the analyzer confirms a caller-side unchecked has no effect on the callee.")]
        [Fact]
        public void Conversions_OverflowCheckIsFixedByTheLibraryNotTheCaller()
        {
            // Arrange
            TestConstant tooWide = new(300, "TooWide");

            // Act & Assert
            _ = Assert.Throws<OverflowException>(() => unchecked(tooWide.ToSByte()));
            _ = Assert.Throws<OverflowException>(() => unchecked((sbyte)tooWide));

            // Narrowing written at the call site does obey the caller, whether it reaches the value
            // through the method or through the widest conversion operator.
            Assert.Equal((sbyte)44, unchecked((sbyte)tooWide.ToIntPtr()));
            Assert.Equal((sbyte)44, unchecked((sbyte)(nint)tooWide));
            _ = Assert.Throws<OverflowException>(() => checked((sbyte)(nint)tooWide));
        }

        /// <summary>
        /// Verifies that the strongly-typed equality requires both the name and the value to match, and
        /// that the name comparison is case-sensitive.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "Asserting the null contract is the point of the test; flow analysis knowing the answer does not make it unworthy of a test.")]
        [Fact]
        public void Equals_TypedOverload_ComparesNameAndValueCaseSensitively()
        {
            // Arrange
            TestConstant constant = new(1, "Alpha");

            // Assert
            Assert.True(constant.Equals(new TestConstant(1, "Alpha")));
            Assert.False(constant.Equals(new TestConstant(2, "Alpha")));
            Assert.False(constant.Equals(new TestConstant(1, "Beta")));
            Assert.False(constant.Equals(new TestConstant(1, "ALPHA")));

            TestConstant? none = null;
            Assert.False(constant.Equals(none));
        }

        /// <summary>
        /// Verifies the first arm of the object equality switch, which delegates to the typed
        /// comparison. Every other equality test binds to the typed overload directly, so without this
        /// the delegating arm goes unexercised -- and it is the arm PowerShell reaches when comparing two
        /// constants, since both operands arrive boxed.
        /// </summary>
        [Fact]
        public void Equals_ObjectOverload_DelegatesToTheTypedComparisonForTheSameFamily()
        {
            // Arrange
            TestConstant constant = new(1, "Alpha");

            // Assert
            Assert.True(constant.Equals((object)constant));
            Assert.True(constant.Equals((object)new TestConstant(1, "Alpha")));
            Assert.False(constant.Equals((object)new TestConstant(1, "Beta")));
            Assert.False(constant.Equals(RESOURCE_TYPE.RT_ICON));
        }

        /// <summary>
        /// Verifies that the object overload compares against a string by name, case-insensitively, which
        /// is what lets PowerShell's equality operator work against these constants.
        /// </summary>
        [Fact]
        public void Equals_ObjectOverload_ComparesStringsByNameCaseInsensitively()
        {
            // Arrange
            TestConstant constant = new(1, "Alpha");

            // Assert
            Assert.True(constant.Equals("Alpha"));
            Assert.True(constant.Equals("ALPHA"));
            Assert.True(constant.Equals("alpha"));
            Assert.False(constant.Equals("Beta"));
        }

        /// <summary>
        /// Verifies that the object overload compares numeric operands against the value, across every
        /// numeric type the switch enumerates.
        /// </summary>
        [Fact]
        public void Equals_ObjectOverload_ComparesNumericOperandsByValue()
        {
            // Arrange
            TestConstant constant = new(42, "Answer");

            // Assert
            Assert.True(constant.Equals((sbyte)42));
            Assert.True(constant.Equals((byte)42));
            Assert.True(constant.Equals((short)42));
            Assert.True(constant.Equals((ushort)42));
            Assert.True(constant.Equals(42));
            Assert.True(constant.Equals(42u));
            Assert.True(constant.Equals(42L));
            Assert.True(constant.Equals(42UL));
            Assert.True(constant.Equals((nint)42));
            Assert.True(constant.Equals((nuint)42));
            Assert.False(constant.Equals(43));
        }

        /// <summary>
        /// Verifies that an operand of a type the switch does not enumerate is simply unequal rather than
        /// throwing.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "Asserting the null contract is the point of the test; flow analysis knowing the answer does not make it unworthy of a test.")]
        [Fact]
        public void Equals_ObjectOverload_ReturnsFalseForUnrelatedTypes()
        {
            // Arrange
            TestConstant constant = new(42, "Answer");

            // Assert
            Assert.False(constant.Equals((object?)null));
            Assert.False(constant.Equals(42.0));
            Assert.False(constant.Equals(42m));
            Assert.False(constant.Equals('*'));
            Assert.False(constant.Equals(new object()));
        }

        /// <summary>
        /// Verifies that an unsigned operand too large to be a native integer is reported as unequal.
        /// Equals must not throw for any operand, so a value that cannot possibly match has to compare
        /// false rather than failing the conversion.
        /// </summary>
        [Fact]
        public void Equals_ObjectOverload_ReturnsFalseForOversizedUnsignedOperands()
        {
            // Arrange
            TestConstant constant = new(42, "Answer");

            // Assert
            Assert.False(constant.Equals(ulong.MaxValue));
            Assert.False(constant.Equals(unchecked((nuint)(-1))));
        }

        /// <summary>
        /// Verifies that the hash code is derived from the value alone, so two constants sharing a value
        /// hash alike even when their names differ and the typed equality rejects them. That is legal but
        /// deliberate, and it means these constants and their string names are not interchangeable as
        /// dictionary keys.
        /// </summary>
        [Fact]
        public void GetHashCode_DependsOnValueOnly()
        {
            // Arrange
            TestConstant alpha = new(7, "Alpha");
            TestConstant beta = new(7, "Beta");

            // Assert
            Assert.Equal(alpha.GetHashCode(), beta.GetHashCode());
            Assert.False(alpha.Equals(beta));
            Assert.NotEqual(alpha.GetHashCode(), new TestConstant(8, "Alpha").GetHashCode());
        }

        /// <summary>
        /// Verifies that the string representation is the constant's name, which is what makes these
        /// usable in messages and in PowerShell output.
        /// </summary>
        [Fact]
        public void ToString_ReturnsTheName()
        {
            // Assert
            Assert.Equal("Alpha", new TestConstant(1, "Alpha").ToString());
        }

        /// <summary>
        /// Verifies that each explicit conversion operator agrees with the corresponding method, so the
        /// cast syntax and the method call cannot drift apart.
        /// </summary>
        [Fact]
        public void ExplicitOperators_AgreeWithTheEquivalentMethods()
        {
            // Arrange
            TestConstant constant = new(42, "Answer");

            // Assert
            Assert.Equal(constant.ToSByte(), (sbyte)constant);
            Assert.Equal(constant.ToByte(), (byte)constant);
            Assert.Equal(constant.ToInt16(), (short)constant);
            Assert.Equal(constant.ToUInt16(), (ushort)constant);
            Assert.Equal(constant.ToInt32(), (int)constant);
            Assert.Equal(constant.ToUInt32(), (uint)constant);
            Assert.Equal(constant.ToInt64(), (long)constant);
            Assert.Equal(constant.ToUInt64(), (ulong)constant);
            Assert.Equal(constant.ToIntPtr(), (nint)constant);
            Assert.Equal(constant.ToString(), (string)constant);
        }

        /// <summary>
        /// Verifies that every explicit conversion operator rejects a null operand rather than throwing
        /// NullReferenceException from inside the conversion.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ExplicitOperators_ThrowArgumentNullExceptionForNullOperand()
        {
            // Arrange
            TypedConstant<TestConstant> constant = null!;

            // Assert
            _ = Assert.Throws<ArgumentNullException>(() => (sbyte)constant);
            _ = Assert.Throws<ArgumentNullException>(() => (byte)constant);
            _ = Assert.Throws<ArgumentNullException>(() => (short)constant);
            _ = Assert.Throws<ArgumentNullException>(() => (ushort)constant);
            _ = Assert.Throws<ArgumentNullException>(() => (int)constant);
            _ = Assert.Throws<ArgumentNullException>(() => (uint)constant);
            _ = Assert.Throws<ArgumentNullException>(() => (long)constant);
            _ = Assert.Throws<ArgumentNullException>(() => (ulong)constant);
            _ = Assert.Throws<ArgumentNullException>(() => (nint)constant);
            _ = Assert.Throws<ArgumentNullException>(() => (string)constant);
        }

        /// <summary>
        /// Verifies that a constant cannot be created without a name, since the name is what every
        /// string comparison and the string representation depend on.
        /// </summary>
        [Fact]
        public void Constructor_RejectsNullName()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new TestConstant(1, name: null));
        }

        /// <summary>
        /// Verifies the behaviour against a real production constant family, so the base class is proven
        /// through the shape the toolkit actually uses rather than only through a test double.
        /// </summary>
        [Fact]
        public void ProductionConstantFamily_ExposesNameAndValue()
        {
            // Assert
            Assert.Equal(nameof(RESOURCE_TYPE.RT_ICON), RESOURCE_TYPE.RT_ICON.ToString());
            Assert.Equal(nameof(RESOURCE_TYPE.RT_MANIFEST), RESOURCE_TYPE.RT_MANIFEST.ToString());
            Assert.True(RESOURCE_TYPE.RT_ICON.Equals(nameof(RESOURCE_TYPE.RT_ICON)));
            Assert.False(RESOURCE_TYPE.RT_ICON.Equals(RESOURCE_TYPE.RT_MANIFEST));
        }
    }
}
