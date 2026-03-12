using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for working with read-only spans of bytes.
    /// </summary>
    internal static class ReadOnlySpanExtensions
    {
        /// <summary>
        /// Interprets the contents of the specified read-only byte span as a reference to a structure of type T.
        /// </summary>
        /// <remarks>This method does not perform any validation on the size or alignment of the span. It
        /// is the caller's responsibility to ensure that the span is large enough and properly aligned for the target
        /// type. Using this method incorrectly can result in undefined behavior.</remarks>
        /// <typeparam name="T">The type of structure to interpret the span as. Must be an unmanaged type.</typeparam>
        /// <param name="span">The read-only span of bytes to interpret as a structure of type T. The span must be at least as large as the
        /// size of T.</param>
        /// <returns>A reference to the structure of type T at the start of the span.</returns>
        internal static ref readonly T AsReadOnlyStructure<T>(this ReadOnlySpan<byte> span) where T : unmanaged
        {
            return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
        }

        /// <summary>
        /// Converts a read-only span of characters to a null-terminated Unicode string.
        /// </summary>
        /// <remarks>This method is intended for use with spans that are guaranteed to contain a valid
        /// Unicode string. Ensure that the span is properly null-terminated to avoid exceptions.</remarks>
        /// <param name="span">The read-only span of characters to convert. The span must represent a valid null-terminated Unicode string.</param>
        /// <returns>A string representation of the provided span. Returns null if the span does not contain a valid
        /// null-terminated Unicode string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the provided span does not contain a valid null-terminated Unicode string.</exception>
        internal static string? ToStringUni(this ReadOnlySpan<char> span)
        {
            int nullTerminator = span.IndexOf('\0');
            if (nullTerminator == -1)
            {
                throw new InvalidOperationException("The provided span does not contain a null-terminated Unicode string.");
            }
            ReadOnlySpan<char> stringSpan = span.Slice(0, nullTerminator).Trim();
            return !stringSpan.IsWhiteSpace() ? stringSpan.ToString() : null;
        }

        /// <summary>
        /// Trims leading and trailing white-space characters from the specified span and removes any trailing null
        /// characters.
        /// </summary>
        /// <remarks>Use this method to sanitize input spans before further processing, ensuring that
        /// extraneous white-space or null characters do not affect subsequent operations.</remarks>
        /// <param name="span">The span of characters to trim. May contain leading or trailing white-space and null characters.</param>
        /// <returns>A read-only span of characters with all leading and trailing white-space removed and any trailing null
        /// characters eliminated.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<char> TrimRemoveEndNull(this ReadOnlySpan<char> span)
        {
            return span.Trim().TrimEnd('\0').TrimEnd();
        }
    }
}
