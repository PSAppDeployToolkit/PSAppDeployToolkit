using System.Numerics;

namespace PSADT.Utilities
{
    /// <summary>
    /// Utility class to convert values to another type by casting (PowerShell can't do this without help).
    /// </summary>
    public static class ValueTypeConverter
    {
        /// <summary>
        /// Converts the given value to a signed byte.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static sbyte ToSByte(BigInteger val) => unchecked((sbyte)val);

        /// <summary>
        /// Converts the given value to an unsigned byte.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static byte ToByte(BigInteger val) => unchecked((byte)val);

        /// <summary>
        /// Converts the given value to a signed short.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static short ToShort(BigInteger val) => unchecked((short)val);

        /// <summary>
        /// Converts the given value to an unsigned short.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ushort ToUShort(BigInteger val) => unchecked((ushort)val);

        /// <summary>
        /// Converts the given value to a signed integer.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int ToInt(BigInteger val) => unchecked((int)val);

        /// <summary>
        /// Converts the given value to an unsigned integer.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static uint ToUInt(BigInteger val) => unchecked((uint)val);

        /// <summary>
        /// Converts the given value to a signed long.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static long ToLong(BigInteger val) => unchecked((long)val);

        /// <summary>
        /// Converts the given value to an unsigned long.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ulong ToULong(BigInteger val) => unchecked((ulong)val);

        /// <summary>
        /// Converts the given value to a signed short.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static short ToInt16(BigInteger val) => ToShort(val);

        /// <summary>
        /// Converts the given value to an unsigned short.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ushort ToUInt16(BigInteger val) => ToUShort(val);

        /// <summary>
        /// Converts the given value to a signed integer.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int ToInt32(BigInteger val) => ToInt(val);

        /// <summary>
        /// Converts the given value to an unsigned integer.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static uint ToUInt32(BigInteger val) => ToUInt(val);

        /// <summary>
        /// Converts the given value to a signed long.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static long ToInt64(BigInteger val) => ToLong(val);

        /// <summary>
        /// Converts the given value to an unsigned long.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ulong ToUInt64(BigInteger val) => ToULong(val);

        /// <summary>
        /// Valid value types for ValueTypeConverter.
        /// </summary>
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
            Long,

            /// <summary>
            /// An unsigned 64-bit integer.
            /// </summary>
            Int64,

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
