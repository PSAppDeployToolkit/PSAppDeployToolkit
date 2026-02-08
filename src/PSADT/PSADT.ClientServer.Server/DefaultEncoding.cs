using System.Text;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides a UTF-8 encoding instance that omits the byte order mark (BOM) and throws an exception on invalid byte
    /// sequences.
    /// </summary>
    /// <remarks>This encoding is configured to strictly enforce UTF-8 compliance by not allowing invalid
    /// bytes and by not emitting a BOM. Use this encoding when interoperability with systems that do not expect a BOM
    /// is required, or when you want to ensure that invalid input is not silently ignored.</remarks>
    internal static class DefaultEncoding
    {
        /// <summary>
        /// Represents a UTF-8 encoding that does not provide a byte order mark (BOM) and throws an exception on invalid
        /// bytes.
        /// </summary>
        /// <remarks>This encoding instance is configured to omit the BOM when encoding and to throw an
        /// exception if invalid byte sequences are encountered during decoding. Use this encoding when you require
        /// strict UTF-8 compliance and want to avoid silent data loss from invalid bytes.</remarks>
        internal static readonly UTF8Encoding Value = new(false, true);
    }
}
