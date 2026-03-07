#if !NET6_0_OR_GREATER
using System.ComponentModel;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfill for StackTraceHiddenAttribute on .NET Framework 4.7.2.
    /// Types and methods attributed with StackTraceHiddenAttribute will be omitted from the stack trace text shown in StackTrace.ToString() and Exception.StackTrace.
    /// </summary>
    /// <remarks>
    /// This attribute is used to hide implementation details from stack traces to make them cleaner and more readable.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class StackTraceHiddenAttribute : Attribute
    {
    }
}
#endif
