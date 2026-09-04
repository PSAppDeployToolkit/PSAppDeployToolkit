using Windows.Win32.Foundation;

namespace PSADT.Interop
{
    /// <summary>
    /// Provides a base class for typed constants whose value is a string pointer rather than a plain
    /// number, such as the resource type, MSI persistence mode and task dialog icon families.
    /// </summary>
    /// <typeparam name="TSelf">The derived type implementing this pattern.</typeparam>
    /// <remarks>
    /// The pointer conversion lives here rather than on <see cref="TypedConstant{TSelf}"/> so that
    /// integer-valued families cannot reach it. Reinterpreting a small integer such as a dialog result
    /// as a string pointer and handing it to native code would not be meaningful, and keeping the two
    /// kinds of constant apart makes that a compile error rather than a caller's problem.
    /// </remarks>
    internal abstract class PointerTypedConstant<TSelf> : TypedConstant<TSelf> where TSelf : PointerTypedConstant<TSelf>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointerTypedConstant{TSelf}"/> class with the
        /// specified <see cref="PCWSTR"/> value.
        /// </summary>
        /// <param name="value">The PCWSTR value to be associated with this instance.</param>
        /// <param name="name">The name of the constant.</param>
        private protected PointerTypedConstant(PCWSTR value, string? name) : base(GetAddress(value), name)
        {
        }

        /// <summary>
        /// Converts this instance to a <see cref="PCWSTR"/> value.
        /// </summary>
        /// <returns>The PCWSTR representation of this constant's value.</returns>
        internal PCWSTR ToPCWSTR()
        {
            unsafe
            {
                return (PCWSTR)(char*)ToIntPtr();
            }
        }

        /// <summary>
        /// Reads the address out of a <see cref="PCWSTR"/> so it can be handed to the base constructor,
        /// which stores every constant's value as a native integer.
        /// </summary>
        /// <param name="value">The PCWSTR value to read.</param>
        /// <returns>The address the PCWSTR refers to.</returns>
        private static nint GetAddress(PCWSTR value)
        {
            unsafe
            {
                return (nint)value.Value;
            }
        }
    }
}
