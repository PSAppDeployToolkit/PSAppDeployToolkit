using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PSADT.Extensions
{
    /// <summary>
    /// Provides extension methods for working with spans of bytes.
    /// </summary>
    internal static class SpanExtensions
    {
        /// <summary>
        /// Interprets the contents of the specified byte span as a reference to a structure of type T.
        /// </summary>
        /// <remarks>This method does not perform any validation on the size or alignment of the span. It
        /// is the caller's responsibility to ensure that the span is large enough and properly aligned for the target
        /// type. Using this method incorrectly can result in undefined behavior.</remarks>
        /// <typeparam name="T">The type of structure to interpret the span as. Must be an unmanaged type.</typeparam>
        /// <param name="span">The span of bytes to interpret as a structure of type T. The span must be at least as large as the
        /// size of T.</param>
        /// <returns>A reference to the structure of type T at the start of the span.</returns>
        internal static ref readonly T AsReadOnlyStructure<T>(this Span<byte> span) where T : unmanaged
        {
            return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
        }
    }
}
