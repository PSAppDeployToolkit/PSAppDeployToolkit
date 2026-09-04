using System;
using System.ComponentModel;
using Xunit;

namespace PSADT.Tests.Extensions
{
    /// <summary>
    /// Tests the description lookup used to turn enumeration members into display text.
    /// </summary>
    /// <remarks>
    /// The lookup insists on a usable description and throws otherwise, which the SMBIOS readers rely on
    /// to surface a missing attribute at the point it is needed rather than passing an empty string on to
    /// a caller. The enumerations below are local to the test so that adding a description to a product
    /// enumeration cannot quietly change what is being asserted here.
    /// </remarks>
    public sealed class EnumExtensionsTests
    {
        /// <summary>
        /// Verifies that a member carrying a description returns it verbatim.
        /// </summary>
        /// <param name="value">The member to look up.</param>
        /// <param name="expected">The description it carries.</param>
        [Theory]
        [InlineData(Described.First, "The first one")]
        [InlineData(Described.Second, "The second one")]
        [InlineData(Described.WithPunctuation, "Has a comma, a full stop. And a dash - too")]
        [InlineData(Described.WithLeadingSpace, "  padded  ")]
        public void GetDescription_ReturnsTheAttributeText(Described value, string expected)
        {
            Assert.Equal(expected, value.GetDescription());
        }

        /// <summary>
        /// Verifies that a member with no description attribute is rejected, rather than falling back to
        /// the member's own name.
        /// </summary>
        [Fact]
        public void GetDescription_RejectsAMemberWithNoAttribute()
        {
            _ = Assert.Throws<ArgumentException>(static () => _ = Described.Undescribed.GetDescription());
        }

        /// <summary>
        /// Verifies that a description consisting only of whitespace is rejected, since it would
        /// otherwise reach a caller as apparently valid display text.
        /// </summary>
        /// <param name="value">The member whose description is unusable.</param>
        [Theory]
        [InlineData(Described.EmptyDescription)]
        [InlineData(Described.WhitespaceDescription)]
        public void GetDescription_RejectsABlankDescription(Described value)
        {
            _ = Assert.Throws<ArgumentException>(() => _ = value.GetDescription());
        }

        /// <summary>
        /// Verifies that a value with no corresponding member is rejected. This is the case that reaches
        /// the lookup when a native API reports a value newer than the enumeration knows about.
        /// </summary>
        /// <param name="value">The undefined value, cast to the enumeration.</param>
        [Theory]
        [InlineData(99)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void GetDescription_RejectsAnUndefinedValue(int value)
        {
            _ = Assert.Throws<ArgumentException>(() => _ = ((Described)value).GetDescription());
        }

        /// <summary>
        /// Verifies that a combination of flags is rejected rather than silently resolving to one of
        /// them, because no single field describes the combination.
        /// </summary>
        [Fact]
        public void GetDescription_RejectsACombinationOfFlags()
        {
            _ = Assert.Throws<ArgumentException>(static () => _ = (DescribedBits.Alpha | DescribedBits.Beta).GetDescription());
        }

        /// <summary>
        /// Verifies that a single flag still resolves, so the rejection above is about the combination
        /// rather than about the enumeration being flags.
        /// </summary>
        [Fact]
        public void GetDescription_ResolvesASingleFlag()
        {
            Assert.Equal("Alpha flag", DescribedBits.Alpha.GetDescription());
        }

        /// <summary>
        /// An enumeration covering each state the description lookup has to distinguish.
        /// </summary>
        public enum Described
        {
            /// <summary>
            /// A member with an ordinary description.
            /// </summary>
            [Description("The first one")]
            First = 0,

            /// <summary>
            /// A second member with an ordinary description.
            /// </summary>
            [Description("The second one")]
            Second = 1,

            /// <summary>
            /// A member whose description contains characters that must not be altered.
            /// </summary>
            [Description("Has a comma, a full stop. And a dash - too")]
            WithPunctuation = 2,

            /// <summary>
            /// A member whose description has significant surrounding whitespace.
            /// </summary>
            [Description("  padded  ")]
            WithLeadingSpace = 3,

            /// <summary>
            /// A member with no description attribute at all.
            /// </summary>
            Undescribed = 4,

            /// <summary>
            /// A member whose description is present but empty.
            /// </summary>
            [Description("")]
            EmptyDescription = 5,

            /// <summary>
            /// A member whose description is present but only whitespace.
            /// </summary>
            [Description("   ")]
            WhitespaceDescription = 6,
        }

        /// <summary>
        /// A bitwise enumeration, to cover looking up a combination that names no single member.
        /// </summary>
        [Flags]
        public enum DescribedBits
        {
            /// <summary>
            /// No flags.
            /// </summary>
            [Description("No flags")]
            None = 0,

            /// <summary>
            /// The first flag.
            /// </summary>
            [Description("Alpha flag")]
            Alpha = 1 << 0,

            /// <summary>
            /// The second flag.
            /// </summary>
            [Description("Beta flag")]
            Beta = 1 << 1,
        }
    }
}
