namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Specifies that a string parameter or property must not be null or consist solely of whitespace characters, but
    /// may be an empty string.
    /// </summary>
    /// <remarks>Use this attribute to enforce input validation where empty strings are permitted, but null
    /// values and strings containing only whitespace are not. This is useful in scenarios where an empty string is a
    /// valid value, such as optional user input fields, but you want to prevent nulls or strings that contain only
    /// spaces or tabs.</remarks>
    public sealed class AllowEmptyButNotNullOrWhiteSpaceAttribute() : ValidateNotEmptyOrWhiteSpaceAttributeBase(allowNull: false, allowEmpty: true);
}
