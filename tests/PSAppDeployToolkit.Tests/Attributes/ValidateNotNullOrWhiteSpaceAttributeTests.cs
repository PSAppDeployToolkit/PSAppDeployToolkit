using System;
using PSAppDeployToolkit.Attributes;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Attributes
{
    /// <summary>
    /// Tests the validator that permits neither absence nor emptiness.
    /// </summary>
    /// <remarks>
    /// The behaviour lives in the base class and is covered by
    /// <see cref="BaseValidateNotEmptyOrWhiteSpaceAttributeTests"/>. This asserts only the pair of flags this subclass
    /// chooses, which is the whole of what it contributes.
    /// </remarks>
    public sealed class ValidateNotNullOrWhiteSpaceAttributeTests
    {
        /// <summary>
        /// Verifies that both absence and emptiness are refused, and content accepted.
        /// </summary>
        [Fact]
        public void Validate_RefusesBothAbsenceAndEmptiness()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => ArgumentAttributes.Validate(new ValidateNotNullOrWhiteSpaceAttribute(), arguments: null));
            _ = Assert.Throws<ArgumentException>(static () => ArgumentAttributes.Validate(new ValidateNotNullOrWhiteSpaceAttribute(), string.Empty));
            ArgumentAttributes.Validate(new ValidateNotNullOrWhiteSpaceAttribute(), "content");
        }
    }
}
