using System;
using System.Runtime.CompilerServices;

namespace PSADT.Interop.Extensions
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
            unsafe
            {
                return ref Unsafe.AsRef<T>((void*)unchecked(handle.ThrowIfZeroOrMinusOne() + offset.ThrowIfNegative()));
            }
        }

        /// <summary>
        /// Creates a read-only span of type T from the specified memory address, length, and optional byte offset.
        /// </summary>
        /// <remarks>The caller is responsible for ensuring that the memory region referenced by handle
        /// and offset is valid and remains accessible for the lifetime of the returned span. Using an invalid or
        /// out-of-bounds handle or offset may result in undefined behavior.</remarks>
        /// <typeparam name="T">The type of elements in the span. Must be an unmanaged type.</typeparam>
        /// <param name="handle">A pointer represented as an IntPtr to the start of the memory region.</param>
        /// <param name="length">The number of elements of type T in the span.</param>
        /// <returns>A ReadOnlySpan{T} representing the specified region of memory.</returns>
        internal static ReadOnlySpan<T> AsReadOnlySpan<T>(this IntPtr handle, int length) where T : unmanaged
        {
            unsafe
            {
                return new ReadOnlySpan<T>((void*)handle.ThrowIfZeroOrMinusOne(), length.ThrowIfZeroOrNegative());
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
            unsafe
            {
                return new ReadOnlySpan<char>((char*)handle.ThrowIfZeroOrMinusOne(), length.ThrowIfZeroOrNegative()).TrimEndNullAndTrim();
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
            ReadOnlySpan<char> stringSpan = handle.ThrowIfZeroOrMinusOne().AsReadOnlyCharSpan(length.ThrowIfZeroOrNegative());
            return stringSpan.IsWhiteSpace()
                ? throw new InvalidOperationException("The specified pointer does not contain a valid string.")
                : stringSpan.ToString();
        }

        /// <summary>
        /// Throws an exception if the specified handle is -1, indicating an invalid pointer; otherwise, returns the
        /// original handle.
        /// </summary>
        /// <param name="handle">The handle to validate. Must not be -1.</param>
        /// <param name="name">The name of the calling member. This value is automatically supplied by the compiler.</param>
        /// <returns>The original handle if it is not -1.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handle"/> is -1, indicating an invalid pointer.</exception>
        internal static nint ThrowIfMinusOne(this nint handle, [CallerMemberName] string name = null!)
        {
            return handle == -1
                ? throw new ArgumentNullException(name, "The specified pointer is not valid.")
                : handle;
        }

        /// <summary>
        /// Validates that the specified native integer handle is neither zero nor minus one, throwing an exception if
        /// the handle is invalid.
        /// </summary>
        /// <remarks>Use this method to ensure that a native handle is valid before performing operations
        /// that require a valid pointer. This is commonly used when working with unmanaged resources or interop
        /// scenarios to prevent invalid pointer usage.</remarks>
        /// <param name="handle">The native integer handle to validate. Must not be zero or minus one.</param>
        /// <param name="name">The name of the parameter or caller to include in the exception message if validation fails.</param>
        /// <returns>The original handle if it is valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handle"/> is zero or minus one, indicating an invalid or null pointer.</exception>
        internal static nint ThrowIfZeroOrMinusOne(this nint handle, [CallerMemberName] string name = null!)
        {
            return handle == IntPtr.Zero || handle == -1
                ? throw new ArgumentNullException(name, "The specified pointer is not valid.")
                : handle;
        }
    }
}
