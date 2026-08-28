using Windows.Win32.Foundation;

namespace PSADT.Interop.Tests.TestHelpers
{
    /// <summary>
    /// A pointer-valued constant family used only by tests, mirroring the shape of the resource and MSI
    /// families.
    /// </summary>
    internal sealed class PointerTestConstant : PointerTypedConstant<PointerTestConstant>
    {
        /// <summary>
        /// Creates a constant with the given string pointer value and name.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <param name="name">The name to store.</param>
        internal PointerTestConstant(PCWSTR value, string? name)
            : base(value, name)
        {
        }
    }
}
