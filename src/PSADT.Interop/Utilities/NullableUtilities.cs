using System;
using System.Runtime.CompilerServices;

namespace PSADT.Interop.Utilities
{
    /// <summary>
    /// Provides methods for working with nullable value types, obtaining pointers to their underlying values.
    /// </summary>
    internal static class NullableUtilities
    {
        /// <summary>
        /// Converts a nullable value type to a pointer to its underlying value.
        /// </summary>
        /// <remarks>Taken by <c language="csharp">ref readonly</c> so the pointer refers to the caller's storage. A value
        /// passed by value is copied into this frame and the pointer dangles the moment the call returns, and this is
        /// the only parameter kind that refuses one: an <c language="csharp">in</c> parameter accepts a value silently
        /// and copies it, where this obliges the caller to write <c language="csharp">in</c> against a variable of
        /// their own.</remarks>
        /// <typeparam name="T">The type of the nullable value.</typeparam>
        /// <param name="value">The nullable value.</param>
        /// <returns>A pointer to the underlying value if it exists; otherwise, null.</returns>
        internal static unsafe T* ToPointer<T>(ref readonly T? value) where T : unmanaged
        {
            return value is not null
                ? (T*)Unsafe.AsPointer(ref Unsafe.AsRef(in Nullable.GetValueRefOrDefaultRef(in value)))
                : null;
        }
    }
}
