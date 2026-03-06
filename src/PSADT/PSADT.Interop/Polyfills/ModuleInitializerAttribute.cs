#if !NET5_0_OR_GREATER
using System.ComponentModel;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfill for ModuleInitializerAttribute on .NET Framework 4.7.2.
    /// Used to indicate that a method should be called when the module is loaded.
    /// </summary>
    /// <remarks>
    /// Methods marked with this attribute must be static, parameterless, non-generic,
    /// return void, and be accessible from the containing module.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif
