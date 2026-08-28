using System;
using PSAppDeployToolkit.Attributes;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Attributes
{
    /// <summary>
    /// Tests the validator that permits absence but not emptiness.
    /// </summary>
    /// <remarks>
    /// The behaviour lives in the base class and is covered by
    /// <see cref="BaseValidateNotEmptyOrWhiteSpaceAttributeTests"/>. This asserts only the pair of flags this subclass
    /// chooses: an optional parameter that has to say something if it says anything.
    /// </remarks>
    public sealed class AllowNullButNotEmptyOrWhiteSpaceAttributeTests
    {
        /// <summary>
        /// Verifies that absence is permitted while emptiness is not.
        /// </summary>
        [Fact]
        public void Validate_PermitsAbsenceButNotEmptiness()
        {
            ArgumentAttributes.Validate(new AllowNullButNotEmptyOrWhiteSpaceAttribute(), arguments: null);
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new AllowNullButNotEmptyOrWhiteSpaceAttribute(), string.Empty));
            ArgumentAttributes.Validate(new AllowNullButNotEmptyOrWhiteSpaceAttribute(), "content");
        }
    }
}
