namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Specifies that a parameter or property must not be empty or consist only of white-space characters if a value is
    /// provided, and enforces that collections are not empty and do not contain null or white-space elements.
    /// </summary>
    /// <remarks>Apply this attribute to parameters or properties to ensure that, if a value is supplied, it
    /// is meaningful and not empty. For collections, this attribute validates that the collection contains at least one
    /// element and that each element is itself not null or, for strings, not empty or white space. Null values are
    /// explicitly allowed, enabling optional parameters that must be non-empty if specified. This attribute is commonly
    /// used in PowerShell cmdlets and functions to enforce input validation at runtime.</remarks>
    public sealed class AllowNullButNotEmptyOrWhiteSpaceAttribute() : ValidateNotEmptyOrWhiteSpaceAttributeBase(allowNull: true);
}
