#if !NETCOREAPP3_0_OR_GREATER
using System.ComponentModel;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics.CodeAnalysis
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfill for DoesNotReturnAttribute on .NET Framework 4.7.2.
    /// Applied to a method that will never return under any circumstance.
    /// </summary>
    /// <remarks>
    /// This attribute informs the compiler and analysis tools that the method
    /// will never return normally, such as methods that always throw an exception.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute
    {
    }
}
#endif
