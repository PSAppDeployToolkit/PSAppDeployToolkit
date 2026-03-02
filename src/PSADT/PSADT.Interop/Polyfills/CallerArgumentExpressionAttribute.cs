#if !NET5_0_OR_GREATER
using System.ComponentModel;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfill for CallerArgumentExpressionAttribute on .NET Framework 4.7.2.
    /// Allows capturing the expression passed to a method parameter as a string at compile time.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CallerArgumentExpressionAttribute"/> class.
    /// </remarks>
    /// <param name="parameterName">The name of the parameter whose expression should be captured.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
    {
        /// <summary>
        /// Gets the name of the parameter whose expression should be captured.
        /// </summary>
        public string ParameterName { get; } = parameterName;
    }
}
#endif
