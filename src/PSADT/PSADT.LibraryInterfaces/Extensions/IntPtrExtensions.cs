using System;
using System.Runtime.CompilerServices;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for working with memory addresses represented by <see cref="nint"/>. These methods
    /// enable interpreting unmanaged memory as structures of a specified type.
    /// </summary>
    /// <remarks>These extension methods are intended for advanced scenarios involving direct memory
    /// manipulation. The caller is responsible for ensuring that memory addresses and offsets are valid and properly
    /// aligned for the target structure type. Improper use may result in undefined behavior or application
    /// instability.</remarks>
    internal static class IntPtrExtensions
    {
        /// <summary>
        /// Returns a reference to a structure of type <typeparamref name="T"/> located at the specified memory address
        /// and offset.
        /// </summary>
        /// <remarks>This method performs no safety checks on the provided memory address or offset. The
        /// caller is responsible for ensuring that the memory is valid and properly aligned for the type <typeparamref
        /// name="T"/>. Using an invalid address or offset may result in undefined behavior.</remarks>
        /// <typeparam name="T">The value type to interpret at the given memory location. Must be an unmanaged structure.</typeparam>
        /// <param name="handle">The base memory address that points to the start of the structure.</param>
        /// <param name="offset">The byte offset from <paramref name="handle"/> at which the structure of type <typeparamref name="T"/> is
        /// located. The offset must be zero or positive.</param>
        /// <returns>A reference to the structure of type <typeparamref name="T"/> at the specified memory address and offset.</returns>
        internal static ref readonly T AsReadOnlyStructure<T>(this nint handle, int offset = 0) where T : unmanaged
        {
            ConfirmNotZeroOrMinusOne(handle);
            ConfirmOffsetIsValid(offset);
            unsafe
            {
                return ref Unsafe.AsRef<T>((void*)unchecked(handle + offset));
            }
        }

        /// <summary>
        /// Converts a native integer handle to a read-only span of characters, trimming any trailing null characters.
        /// </summary>
        /// <remarks>This method is intended for use with unmanaged memory and should be used with
        /// caution. Ensure that the handle points to valid memory containing character data.</remarks>
        /// <param name="handle">The native integer handle pointing to a memory location containing character data.</param>
        /// <param name="length">The number of characters to read from the memory location specified by the handle.</param>
        /// <returns>A read-only span of characters representing the data at the specified handle, trimmed of any trailing null
        /// characters.</returns>
        internal static ReadOnlySpan<char> AsReadOnlyCharSpan(this nint handle, int length)
        {
            ConfirmNotZeroOrMinusOne(handle);
            ConfirmLengthIsValid(length);
            unsafe
            {
                return new ReadOnlySpan<char>((char*)handle, length).TrimEndNullAndTrim();
            }
        }

        /// <summary>
        /// Converts a pointer to unmanaged memory into a managed string representation using the specified length.
        /// </summary>
        /// <remarks>This method is intended for use with unmanaged memory. Ensure that <paramref
        /// name="handle"/> points to a valid memory location containing string data, and that <paramref name="length"/>
        /// accurately reflects the number of characters to read. Use caution when working with unmanaged pointers to
        /// avoid memory access violations.</remarks>
        /// <param name="handle">A pointer to the unmanaged memory containing the string data to convert.</param>
        /// <param name="length">The number of characters to read from the memory pointed to by <paramref name="handle"/>.</param>
        /// <returns>A managed string containing the characters read from the specified memory location. Returns an empty string
        /// if the memory does not contain valid string data.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the specified pointer does not reference valid string data or if the length is zero.</exception>
        internal static string ToManagedString(this nint handle, int length)
        {
            ConfirmNotZeroOrMinusOne(handle); ConfirmLengthIsValid(length);
            ReadOnlySpan<char> stringSpan = handle.AsReadOnlyCharSpan(length);
            return stringSpan.IsWhiteSpace()
                ? throw new InvalidOperationException("The specified pointer does not contain a valid string.")
                : stringSpan.ToString();
        }

        /// <summary>
        /// Validates that the specified pointer is neither zero nor negative one, ensuring it is a valid handle.
        /// </summary>
        /// <param name="handle">The handle to validate. Must not be zero or negative one.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handle"/> is zero or negative one.</exception>
        private static void ConfirmNotZeroOrMinusOne(nint handle)
        {
            if (handle == IntPtr.Zero || handle == -1)
            {
                throw new ArgumentNullException("The specified pointer is not valid.", (Exception?)null);
            }
        }

        /// <summary>
        /// Validates that the specified offset is not negative.
        /// </summary>
        /// <param name="offset">The offset value to validate. Must be zero or greater.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> is less than zero.</exception>
        private static void ConfirmOffsetIsValid(int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("The specified offset is not valid", (Exception?)null);
            }
        }

        /// <summary>
        /// Validates that the specified length is greater than zero.
        /// </summary>
        /// <param name="length">The length to validate. Must be a positive integer.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="length"/> is less than or equal to zero.</exception>
        private static void ConfirmLengthIsValid(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("The specified length is not valid", (Exception?)null);
            }
        }
    }
}
