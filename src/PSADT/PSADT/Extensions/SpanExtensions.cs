using System;
using System.Runtime.InteropServices;

namespace PSADT.Extensions
{
    /// <summary>
    /// Provides extension methods for working with spans of bytes.
    /// </summary>
    /// <remarks>These methods enable direct manipulation of byte spans, such as writing struct values into
    /// memory. All methods are intended for internal use and do not perform bounds checking; callers must ensure
    /// correct usage to avoid undefined behavior.</remarks>
    internal static class SpanExtensions
    {
        /// <summary>
        /// Writes the value of the specified struct to the beginning of the provided byte span.
        /// </summary>
        /// <remarks>The bytes of the value are written in the default memory layout of the struct. The
        /// caller is responsible for ensuring that the span is at least as large as sizeof(T); otherwise, the behavior
        /// is undefined. This method does not perform bounds checking.</remarks>
        /// <typeparam name="T">The type of the value to write. Must be an unmanaged struct.</typeparam>
        /// <param name="span">The span of bytes to which the value will be written. Must be large enough to contain the value of type T.</param>
        /// <param name="value">A reference to the value to write into the span.</param>
        internal static void Write<T>(this Span<byte> span, ref T value) where T : struct
        {
#if NET8_0_OR_GREATER
            MemoryMarshal.Write(span, in value);
#else
            MemoryMarshal.Write(span, ref value);
#endif
        }
    }
}
