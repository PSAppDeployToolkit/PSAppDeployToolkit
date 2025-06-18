using System;
using System.Text;

namespace PSADT.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="Encoding"/> class to simplify working with byte spans.
    /// </summary>
    /// <remarks>This class contains methods that extend the functionality of <see cref="Encoding"/> to
    /// support operations on <see cref="Span{T}"/> instances, enabling efficient manipulation of byte data without
    /// requiring additional allocations.</remarks>
    internal static class EncodingExtensions
    {
        /// <summary>
        /// Decodes a sequence of bytes from the specified <see cref="Encoding"/> into a string.
        /// </summary>
        /// <remarks>This method provides a convenient way to decode a span of bytes using the specified
        /// encoding. If <paramref name="byteCount"/> is provided, only the specified number of bytes from the span will
        /// be decoded; otherwise, the entire span is used.</remarks>
        /// <param name="encoding">The <see cref="Encoding"/> used to decode the byte sequence.</param>
        /// <param name="bytes">A span of bytes to decode.</param>
        /// <param name="byteCount">An optional number of bytes to decode. If <see langword="null"/>, the entire span is decoded.</param>
        /// <returns>A string containing the decoded characters from the specified byte sequence.</returns>
        internal static unsafe string GetString(this Encoding encoding, Span<byte> bytes, int? byteCount = null)
        {
            fixed (byte* pBytes = bytes)
            {
                return encoding.GetString(pBytes, byteCount ?? bytes.Length);
            }
        }
    }
}
