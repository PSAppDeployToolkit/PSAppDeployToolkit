using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSAppDeployToolkit.Attributes;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Attributes
{
    /// <summary>
    /// Tests the validator that insists a value is above zero.
    /// </summary>
    /// <remarks>
    /// Applied to timeouts and retry counts, where zero means something different from unset and is almost always a
    /// caller mistake. It reaches zero two ways: a value type is compared against its own default, and a reference
    /// type against a static <c language="csharp">Zero</c> of its own type.
    /// </remarks>
    public sealed class ValidateGreaterThanZeroAttributeTests
    {
        /// <summary>
        /// Verifies that a value above zero is accepted, for each numeric type a parameter might use.
        /// </summary>
        [Fact]
        public void Validate_AcceptsAValueAboveZero()
        {
            foreach (object value in new object[] { 1, 1u, 1L, 1uL, (short)1, (ushort)1, (byte)1, (sbyte)1, 0.1f, 0.1d, 0.1m, TimeSpan.FromTicks(1) })
            {
                ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), value);
            }
        }

        /// <summary>
        /// Verifies that zero itself is refused, and named as out of range rather than as the wrong type.
        /// </summary>
        [Fact]
        public void Validate_RefusesZero()
        {
            foreach (object value in new object[] { 0, 0u, 0L, 0d, 0m, TimeSpan.Zero })
            {
                _ = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), value));
            }
        }

        /// <summary>
        /// Verifies that a value below zero is refused.
        /// </summary>
        [Fact]
        public void Validate_RefusesAValueBelowZero()
        {
            foreach (object value in new object[] { -1, -1L, -0.1d, -0.1m, TimeSpan.FromTicks(-1) })
            {
                _ = Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), value));
            }
        }

        /// <summary>
        /// Verifies that a collection is checked element by element.
        /// </summary>
        [Fact]
        public void Validate_ChecksEveryElementOfACollection()
        {
            ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), new[] { 1, 2, 3 });
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), new[] { 1, 0, 3 }));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), new List<TimeSpan> { TimeSpan.FromSeconds(1), TimeSpan.Zero }));
        }

        /// <summary>
        /// Verifies that a collection carrying nothing at all is refused as null rather than as out of range.
        /// </summary>
        [Fact]
        public void Validate_RefusesACollectionCarryingNothing()
        {
            Assert.Contains(
                "collection contains a null element",
                Assert.Throws<ArgumentNullException>(static () => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), new object?[] { 1, null })).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that nothing at all is refused.
        /// </summary>
        [Fact]
        public void Validate_RefusesNothingAtAll()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), arguments: null));
        }

        /// <summary>
        /// Verifies that a type with no notion of zero is refused for that reason, and says which type it was.
        /// </summary>
        /// <remarks>
        /// A reference type qualifies only by carrying a static <c language="csharp">Zero</c> of its own type. Neither of these does, so
        /// both are refused as unsupported rather than silently passed - which is the safer failure, since a caller
        /// would otherwise believe a check had happened.
        /// </remarks>
        [Fact]
        public void Validate_RefusesATypeWithNoNotionOfZero()
        {
            Assert.Contains(
                "does not support greater-than-zero validation",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), "text")).Message,
                StringComparison.Ordinal);
            Assert.Contains(
                "System.Version",
                Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), new Version(1, 0))).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a reference type carrying a static <c language="csharp">Zero</c> of its own type is compared against it.
        /// </summary>
        /// <remarks>
        /// Reached with a stand-in because nothing in the framework fits: the types that offer a static <c language="csharp">Zero</c> -
        /// <see cref="TimeSpan"/>, <see cref="decimal"/>, the numeric types - are all value types and take the other
        /// path. So this branch would otherwise never run.
        /// </remarks>
        [Fact]
        public void Validate_ComparesAReferenceTypeAgainstItsOwnZero()
        {
            ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), new Quantity(1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), new Quantity(0)));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), new Quantity(-1)));
        }

        /// <summary>
        /// Verifies that a value wrapped by PowerShell is unwrapped before being compared.
        /// </summary>
        [Fact]
        public void Validate_UnwrapsAPSObject()
        {
            ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), PSObject.AsPSObject(1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => ArgumentAttributes.Validate(new ValidateGreaterThanZeroAttribute(), PSObject.AsPSObject(0)));
        }

        /// <summary>
        /// A comparable reference type offering a static <c language="csharp">Zero</c> of its own type.
        /// </summary>
        /// <param name="value">The amount this stands for.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0097:A class that implements IComparable<T> or IComparable should override comparison operators", Justification = "The validator compares through IComparable by reflection; operators would never be called.")]
        private sealed class Quantity(int value) : IComparable
        {
            /// <summary>
            /// The zero the validator compares against.
            /// </summary>
            public static Quantity Zero { get; } = new(0);

            /// <inheritdoc/>
            public int CompareTo(object? obj)
            {
                return obj is Quantity other
                    ? _value.CompareTo(other._value)
                    : throw new ArgumentException("Not a quantity.", nameof(obj));
            }

            /// <summary>
            /// The amount held.
            /// </summary>
            private readonly int _value = value;
        }
    }
}
