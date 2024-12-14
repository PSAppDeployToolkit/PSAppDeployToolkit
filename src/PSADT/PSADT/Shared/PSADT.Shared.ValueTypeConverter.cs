namespace PSADT.Shared
{
    public static class ValueTypeConverter
    {
        public static sbyte ToSByte(long val)
        {
            unchecked
            {
                return (sbyte)val;
            }
        }

        public static byte ToByte(long val)
        {
            unchecked
            {
                return (byte)val;
            }
        }

        public static short ToShort(long val)
        {
            unchecked
            {
                return (short)val;
            }
        }

        public static ushort ToUShort(long val)
        {
            unchecked
            {
                return (ushort)val;
            }
        }

        public static int ToInt(long val)
        {
            unchecked
            {
                return (int)val;
            }
        }

        public static uint ToUInt(long val)
        {
            unchecked
            {
                return (uint)val;
            }
        }

        public static long ToLong(long val)
        {
            unchecked
            {
                return (long)val;
            }
        }

        public static ulong ToULong(long val)
        {
            unchecked
            {
                return (ulong)val;
            }
        }

        public static short ToInt16(long val) { return ToShort(val); }
        public static ushort ToUInt16(long val) { return ToUShort(val); }
        public static int ToInt32(long val) { return ToInt(val); }
        public static uint ToUInt32(long val) { return ToUInt(val); }
        public static long ToInt64(long val) { return ToLong(val); }
        public static ulong ToUInt64(long val) { return ToULong(val); }
    }
}
