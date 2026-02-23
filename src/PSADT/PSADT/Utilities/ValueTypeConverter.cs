namespace PSADT.Utilities
{
    /// <summary>
    /// Utility class to convert values to another type by casting (PowerShell can't do this without help).
    /// </summary>
    public static class ValueTypeConverter
    {
        /// <summary>
        /// Converts a 64-bit signed integer to an 8-bit signed integer.
        /// </summary>
        /// <remarks>This method performs an unchecked conversion. If the input value exceeds the range of
        /// an 8-bit signed integer (from -128 to 127), the result wraps around without throwing an exception.</remarks>
        /// <param name="val">The 64-bit signed integer to convert.</param>
        /// <returns>An 8-bit signed integer that represents the converted value. If the input value is outside the range of an
        /// 8-bit signed integer, the result is truncated.</returns>
        public static sbyte ToSByte(long val)
        {
            return unchecked((sbyte)val);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to its equivalent 8-bit unsigned integer value.
        /// </summary>
        /// <remarks>This method performs an unchecked conversion. If the input value is less than 0 or
        /// greater than 255, the result will wrap around according to the rules of unchecked casting in C#; no
        /// exception is thrown.</remarks>
        /// <param name="val">The 64-bit signed integer to convert. Values outside the range of 0 to 255 will be truncated to fit within
        /// the byte range.</param>
        /// <returns>An 8-bit unsigned integer that represents the converted value of the input parameter.</returns>
        public static byte ToByte(long val)
        {
            return unchecked((byte)val);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to its equivalent 16-bit signed integer representation.
        /// </summary>
        /// <remarks>This method performs an unchecked conversion. If <paramref name="val"/> exceeds the
        /// range of a 16-bit signed integer, the returned value wraps around without throwing an exception.</remarks>
        /// <param name="val">The 64-bit signed integer value to convert.</param>
        /// <returns>A 16-bit signed integer that represents the converted value of <paramref name="val"/>. If the value of
        /// <paramref name="val"/> is outside the range of a 16-bit signed integer, the result is truncated.</returns>
        public static short ToShort(long val)
        {
            return unchecked((short)val);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to a 16-bit unsigned integer, discarding any higher-order bits that do not
        /// fit in the target type.
        /// </summary>
        /// <remarks>This method performs an unchecked conversion. If the input value is outside the range
        /// of a 16-bit unsigned integer (0 to 65,535), only the least significant 16 bits are used and higher-order
        /// bits are discarded. No exception is thrown if the value is out of range.</remarks>
        /// <param name="val">The 64-bit signed integer value to convert to an unsigned 16-bit integer.</param>
        /// <returns>A 16-bit unsigned integer that represents the lower 16 bits of the specified value.</returns>
        public static ushort ToUShort(long val)
        {
            return unchecked((ushort)val);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to its 32-bit signed integer equivalent.
        /// </summary>
        /// <remarks>If the specified value is outside the range of a 32-bit signed integer, the
        /// conversion wraps around and does not throw an exception.</remarks>
        /// <param name="val">The 64-bit signed integer value to convert to a 32-bit signed integer.</param>
        /// <returns>A 32-bit signed integer that is equivalent to the specified value.</returns>
        public static int ToInt(long val)
        {
            return unchecked((int)val);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to a 32-bit unsigned integer.
        /// </summary>
        /// <remarks>This method performs an unchecked conversion. If the input value is negative or
        /// exceeds the range of a 32-bit unsigned integer, the resulting value is calculated by discarding higher-order
        /// bits.</remarks>
        /// <param name="val">The 64-bit signed integer value to convert.</param>
        /// <returns>A 32-bit unsigned integer that represents the converted value of <paramref name="val"/>. If <paramref
        /// name="val"/> is outside the range of a 32-bit unsigned integer, the result wraps around without throwing an
        /// exception.</returns>
        public static uint ToUInt(long val)
        {
            return unchecked((uint)val);
        }

        /// <summary>
        /// Converts a signed 64-bit integer to its equivalent unsigned 64-bit integer representation.
        /// </summary>
        /// <remarks>This method performs an unchecked conversion. No exception is thrown if the input
        /// value is negative; instead, the bit pattern is reinterpreted as an unsigned value, which may result in
        /// unexpected large positive numbers.</remarks>
        /// <param name="val">The signed 64-bit integer value to convert.</param>
        /// <returns>An unsigned 64-bit integer that represents the input value. If the input is negative, the result will be a
        /// large positive number due to binary representation.</returns>
        public static ulong ToULong(long val)
        {
            return unchecked((ulong)val);
        }

        /// <summary>
        /// Converts the specified 64-bit signed integer to its equivalent 16-bit signed integer.
        /// </summary>
        /// <remarks>If the specified value is outside the range of a 16-bit signed integer, an
        /// OverflowException will be thrown.</remarks>
        /// <param name="val">The 64-bit signed integer value to convert. This value must be within the range of a 16-bit signed integer
        /// (-32,768 to 32,767).</param>
        /// <returns>The 16-bit signed integer equivalent of the specified 64-bit signed integer.</returns>
        public static short ToInt16(long val)
        {
            return ToShort(val);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to its equivalent 16-bit unsigned integer representation.
        /// </summary>
        /// <remarks>If the specified value is less than 0 or greater than 65,535, an exception is
        /// thrown.</remarks>
        /// <param name="val">The 64-bit signed integer value to convert. The value must be in the range of a 16-bit unsigned integer (0
        /// to 65,535).</param>
        /// <returns>A 16-bit unsigned integer that is equivalent to the specified value.</returns>
        public static ushort ToUInt16(long val)
        {
            return ToUShort(val);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to its 32-bit signed integer equivalent.
        /// </summary>
        /// <remarks>An OverflowException is thrown if the value of <paramref name="val"/> is less than
        /// <see cref="int.MinValue"/> or greater than <see cref="int.MaxValue"/>.</remarks>
        /// <param name="val">The 64-bit signed integer to convert. Must be within the range of a 32-bit signed integer.</param>
        /// <returns>A 32-bit signed integer that is equivalent to the specified 64-bit signed integer.</returns>
        public static int ToInt32(long val)
        {
            return ToInt(val);
        }

        /// <summary>
        /// Converts a 64-bit signed integer to its equivalent 32-bit unsigned integer representation.
        /// </summary>
        /// <remarks>An exception is thrown if the input value is less than zero or greater than
        /// UInt32.MaxValue.</remarks>
        /// <param name="val">The 64-bit signed integer to convert. Must be greater than or equal to 0 and less than or equal to
        /// 4,294,967,295.</param>
        /// <returns>A 32-bit unsigned integer that is equivalent to the specified value.</returns>
        public static uint ToUInt32(long val)
        {
            return ToUInt(val);
        }

        /// <summary>
        /// Converts a signed 64-bit integer to its equivalent unsigned 64-bit integer representation.
        /// </summary>
        /// <param name="val">The signed 64-bit integer value to convert.</param>
        /// <returns>An unsigned 64-bit integer that represents the converted value of <paramref name="val"/>.</returns>
        public static ulong ToUInt64(long val)
        {
            return ToULong(val);
        }

        /// <summary>
        /// Valid value types for ValueTypeConverter.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "This is necessary by way of what the enum is and represents.")]
        public enum ValueTypes
        {
            /// <summary>
            /// A signed byte.
            /// </summary>
            SByte,

            /// <summary>
            /// An unsigned byte.
            /// </summary>
            Byte,

            /// <summary>
            /// A signed 16-bit integer.
            /// </summary>
            Short,

            /// <summary>
            /// An unsigned 16-bit integer.
            /// </summary>
            Int16,

            /// <summary>
            /// A signed 32-bit integer.
            /// </summary>
            UShort,

            /// <summary>
            /// An unsigned 32-bit integer.
            /// </summary>
            UInt16,

            /// <summary>
            /// A signed 32-bit integer.
            /// </summary>
            Int,

            /// <summary>
            /// An unsigned 32-bit integer.
            /// </summary>
            Int32,

            /// <summary>
            /// A signed 64-bit integer.
            /// </summary>
            UInt,

            /// <summary>
            /// An unsigned 64-bit integer.
            /// </summary>
            UInt32,

            /// <summary>
            /// A signed 64-bit integer.
            /// </summary>
            ULong,

            /// <summary>
            /// An unsigned 64-bit integer.
            /// </summary>
            UInt64,
        }
    }
}
