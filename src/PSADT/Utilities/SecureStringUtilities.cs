using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides helpers for moving <see cref="SecureString"/> contents across a process boundary.
    /// </summary>
    /// <remarks>
    /// A <see cref="SecureString"/> cannot itself be transmitted. Its memory protection is scoped to the process
    /// that created it and it has no serialisation surface, so every transfer is necessarily an unprotect to
    /// bytes, a carry over an encrypted channel, and a rebuild at the far end.
    /// </remarks>
    [SuppressMessage("Design", "MA0182:Internal type is apparently never used", Justification = "Consumed by PSADT.UserInterface through InternalsVisibleTo.")]
    internal static class SecureStringUtilities
    {
        /// <summary>
        /// Copies the contents of a <see cref="SecureString"/> into a UTF-16 byte array.
        /// </summary>
        /// <param name="value">The value to unprotect. Cannot be null.</param>
        /// <returns>The unprotected UTF-16 bytes. The caller should zero these once they are no longer needed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        internal static byte[] ToUtf16Bytes(SecureString value)
        {
            ArgumentNullException.ThrowIfNull(value);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(value);
                byte[] bytes = new byte[value.Length * sizeof(char)];
                Marshal.Copy(ptr, bytes, 0, bytes.Length);
                return bytes;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }

        /// <summary>
        /// Rebuilds a <see cref="SecureString"/> from its UTF-16 byte representation.
        /// </summary>
        /// <param name="bytes">The UTF-16 bytes to protect. Cannot be null and must hold whole characters.</param>
        /// <returns>A read-only <see cref="SecureString"/> holding the supplied value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bytes"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="bytes"/> holds a partial character.</exception>
        internal static SecureString FromUtf16Bytes(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if ((bytes.Length % sizeof(char)) is not 0)
            {
                throw new ArgumentException("The supplied buffer does not hold a whole number of UTF-16 characters.", nameof(bytes));
            }
            SecureString result = new();
            try
            {
                for (int i = 0; i < bytes.Length; i += sizeof(char))
                {
                    result.AppendChar((char)(bytes[i] | (bytes[i + 1] << 8)));
                }
                result.MakeReadOnly();
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

    }
}
