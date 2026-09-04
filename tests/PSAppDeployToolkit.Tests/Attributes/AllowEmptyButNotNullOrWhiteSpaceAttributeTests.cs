using System;
using PSAppDeployToolkit.Attributes;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Attributes
{
    /// <summary>
    /// Tests the validator that permits emptiness but not absence.
    /// </summary>
    /// <remarks>
    /// The behaviour lives in the base class and is covered by
    /// <see cref="BaseValidateNotEmptyOrWhiteSpaceAttributeTests"/>. This asserts only the pair of flags this subclass
    /// chooses, plus the one place the name misleads: an empty string is permitted, an empty collection is not.
    /// </remarks>
    public sealed class AllowEmptyButNotNullOrWhiteSpaceAttributeTests
    {
        /// <summary>
        /// Verifies that emptiness is permitted while absence is not.
        /// </summary>
        [Fact]
        public void Validate_PermitsEmptinessButNotAbsence()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => ArgumentAttributes.Validate(new AllowEmptyButNotNullOrWhiteSpaceAttribute(), arguments: null));
            ArgumentAttributes.Validate(new AllowEmptyButNotNullOrWhiteSpaceAttribute(), string.Empty);
            ArgumentAttributes.Validate(new AllowEmptyButNotNullOrWhiteSpaceAttribute(), "content");
        }

        /// <summary>
        /// Verifies that permitting an empty string does not permit an empty collection.
        /// </summary>
        [Fact]
        public void Validate_StillRefusesAnEmptyCollection()
        {
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new AllowEmptyButNotNullOrWhiteSpaceAttribute(), Array.Empty<string>()));
        }
    }
}
