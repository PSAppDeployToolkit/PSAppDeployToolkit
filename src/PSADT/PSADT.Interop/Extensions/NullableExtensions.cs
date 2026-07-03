using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides extension methods for working with nullable value types. These methods enable obtaining pointers to the underlying values of nullable structs, facilitating advanced memory manipulation scenarios.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1110:Declare type inside namespace", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0047:Declare types in namespaces", Justification = "Polyfills aren't meant to be part of a namespace.")]
internal static class NullableExtensions
{
    /// <summary>
    /// Converts a nullable value type to a pointer to its underlying value. If the nullable is null, the method returns null. This method is useful for scenarios where you need to work with pointers to unmanaged types.
    /// </summary>
    /// <typeparam name="T">The type of the nullable value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <returns>A pointer to the underlying value if it exists; otherwise, null.</returns>
    internal static unsafe T* ToPointer<T>(this T? value) where T : unmanaged
    {
        return value is not null
            ? (T*)Unsafe.AsPointer(ref Unsafe.AsRef(in Nullable.GetValueRefOrDefaultRef(in value)))
            : null;
    }
}
