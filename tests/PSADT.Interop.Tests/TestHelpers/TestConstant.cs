namespace PSADT.Interop.Tests.TestHelpers
{
    /// <summary>
    /// A constant family used only by tests, so values that the production families never hold can still
    /// be exercised.
    /// </summary>
    internal sealed class TestConstant : TypedConstant<TestConstant>
    {
        /// <summary>
        /// Creates a constant with the given native integer value and name.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <param name="name">The name to store.</param>
        internal TestConstant(nint value, string? name)
            : base(value, name)
        {
        }
    }
}
