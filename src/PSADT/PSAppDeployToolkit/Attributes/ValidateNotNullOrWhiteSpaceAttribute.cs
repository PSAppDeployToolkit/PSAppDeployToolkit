namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Specifies that a parameter or property must not be null, empty, or consist only of white-space characters. For
    /// collections, ensures that the collection is not empty and that each element is valid according to the same
    /// criteria.
    /// </summary>
    /// <remarks>Apply this attribute to parameters or properties to enforce validation rules that prevent
    /// null, empty, or white-space-only values. When applied to collections, the attribute also validates that the
    /// collection is not empty and that each element is not null, empty, or white space. This attribute is commonly
    /// used in PowerShell cmdlets and functions to ensure that required arguments are provided and meet basic content
    /// requirements.</remarks>
    public sealed class ValidateNotNullOrWhiteSpaceAttribute() : ValidateNotEmptyOrWhiteSpaceAttributeBase(allowNull: false);
}
