using System.Management.Automation;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Validates that the value of a parameter is not null, empty, or consists only of white-space characters.
    /// </summary>
    /// <remarks>
    /// This attribute provides <see cref="ValidateNotNullOrWhiteSpaceAttribute"/> functionality for Windows PowerShell 5.1,
    /// which lacks the built-in attribute available in PowerShell 7+. Since this attribute is in a different namespace
    /// (<c>PSAppDeployToolkit.Foundation</c> vs <c>System.Management.Automation</c>), it does not conflict with the
    /// built-in attribute when the .NET Framework assembly is loaded in PowerShell 7.
    /// </remarks>
    public sealed class ValidateNotNullOrWhiteSpaceAttribute : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Validates that the argument is not null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="arguments">The argument value to validate.</param>
        /// <param name="engineIntrinsics">Provides access to the PowerShell engine APIs.</param>
        /// <exception cref="ValidationMetadataException">
        /// Thrown when <paramref name="arguments"/> is null, an empty string, or a string that consists only of white-space characters.
        /// </exception>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments is null)
            {
                throw new ValidationMetadataException("The argument is null. Provide a valid value for the argument, and then try running the command again.");
            }
            if (arguments is string str && string.IsNullOrWhiteSpace(str))
            {
                throw new ValidationMetadataException("The argument is null or white space. Provide an argument that is not null or white space, and then try running the command again.");
            }
        }
    }
}
