using System;

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
        public static sbyte ToSByte(long val)
        {
            unchecked
            {
                return (sbyte)val;
            }
        }

        /// <summary>
        /// Converts the given value to an unsigned byte.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static byte ToByte(long val)
        {
            unchecked
            {
                return (byte)val;
            }
        }

        /// <summary>
        /// Converts the given value to a signed short.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static short ToShort(long val)
        {
            unchecked
            {
                return (short)val;
            }
        }

        /// <summary>
        /// Converts the given value to an unsigned short.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ushort ToUShort(long val)
        {
            unchecked
            {
                return (ushort)val;
            }
        }

        /// <summary>
        /// Converts the given value to a signed integer.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int ToInt(long val)
        {
            unchecked
            {
                return (int)val;
            }
        }

        /// <summary>
        /// Converts the given value to an unsigned integer.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static uint ToUInt(long val)
        {
            unchecked
            {
                return (uint)val;
            }
        }

        /// <summary>
        /// Converts the given value to a signed long.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static long ToLong(long val)
        {
            unchecked
            {
                return val;
            }
        }

        /// <summary>
        /// Converts the given value to an unsigned long.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ulong ToULong(long val)
        {
            unchecked
            {
                return (ulong)val;
            }
        }

        /// <summary>
        /// Converts the given value to a signed short.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static short ToInt16(long val) { return ToShort(val); }

        /// <summary>
        /// Converts the given value to an unsigned short.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ushort ToUInt16(long val) { return ToUShort(val); }

        /// <summary>
        /// Converts the given value to a signed integer.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int ToInt32(long val) { return ToInt(val); }

        /// <summary>
        /// Converts the given value to an unsigned integer.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static uint ToUInt32(long val) { return ToUInt(val); }

        /// <summary>
        /// Converts the given value to a signed long.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static long ToInt64(long val) { return ToLong(val); }

        /// <summary>
        /// Converts the given value to an unsigned long.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ulong ToUInt64(long val) { return ToULong(val); }

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

    /// <summary>
    /// Utility class to convert values to another type by casting (PowerShell can't do this without help).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class ValueTypeConverter<T>
    {
        /// <summary>
        /// Converts the given value to the specified type.
        /// </summary>
        public static readonly Func<long, T> Convert;

        /// <summary>
        /// Initializes the <see cref="ValueTypeConverter{T}"/> class.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        static ValueTypeConverter()
        {
            if (typeof(T) == typeof(sbyte))
            {
                Convert = (Func<long, T>)(object)(Func<long, sbyte>)ValueTypeConverter.ToSByte;
            }
            else if (typeof(T) == typeof(byte))
            {
                Convert = (Func<long, T>)(object)(Func<long, byte>)ValueTypeConverter.ToByte;
            }
            else if (typeof(T) == typeof(short))
            {
                Convert = (Func<long, T>)(object)(Func<long, short>)ValueTypeConverter.ToShort;
            }
            else if (typeof(T) == typeof(ushort))
            {
                Convert = (Func<long, T>)(object)(Func<long, ushort>)ValueTypeConverter.ToUShort;
            }
            else if (typeof(T) == typeof(int))
            {
                Convert = (Func<long, T>)(object)(Func<long, int>)ValueTypeConverter.ToInt;
            }
            else if (typeof(T) == typeof(uint))
            {
                Convert = (Func<long, T>)(object)(Func<long, uint>)ValueTypeConverter.ToUInt;
            }
            else if (typeof(T) == typeof(long))
            {
                Convert = (Func<long, T>)(object)(Func<long, long>)ValueTypeConverter.ToLong;
            }
            else if (typeof(T) == typeof(ulong))
            {
                Convert = (Func<long, T>)(object)(Func<long, ulong>)ValueTypeConverter.ToULong;
            }
            else
            {
                throw new NotSupportedException($"Type {typeof(T)} is not supported.");
            }
        }
    }
}
