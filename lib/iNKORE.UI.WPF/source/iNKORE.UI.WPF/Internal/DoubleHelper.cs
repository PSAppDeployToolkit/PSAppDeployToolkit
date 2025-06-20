using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace iNKORE._Internal
{
    internal static class DoubleHelper
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct NanUnion
        {
            [FieldOffset(0)]
            internal double DoubleValue;

            [FieldOffset(0)]
            internal ulong UintValue;
        }

        internal const double DBL_EPSILON = 2.220446049250313E-16;

        internal const float FLT_MIN = 1.1754944E-38f;

        public static bool AreClose(this double value1, double value2)
        {
            if (value1 == value2)
            {
                return true;
            }
            double num = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.220446049250313E-16;
            double num2 = value1 - value2;
            if (0.0 - num < num2)
            {
                return num > num2;
            }
            return false;
        }

        public static bool LessThan(this double value1, double value2)
        {
            if (value1 < value2)
            {
                return !AreClose(value1, value2);
            }
            return false;
        }

        public static bool GreaterThan(this double value1, double value2)
        {
            if (value1 > value2)
            {
                return !AreClose(value1, value2);
            }
            return false;
        }

        public static bool LessThanOrClose(this double value1, double value2)
        {
            if (!(value1 < value2))
            {
                return AreClose(value1, value2);
            }
            return true;
        }

        public static bool GreaterThanOrClose(this double value1, double value2)
        {
            if (!(value1 > value2))
            {
                return AreClose(value1, value2);
            }
            return true;
        }

        public static bool IsOne(this double value)
        {
            return Math.Abs(value - 1.0) < 2.220446049250313E-15;
        }

        public static bool IsZero(this double value)
        {
            return Math.Abs(value) < 2.220446049250313E-15;
        }


        public static bool IsBetweenZeroAndOne(this double val)
        {
            if (GreaterThanOrClose(val, 0.0))
            {
                return LessThanOrClose(val, 1.0);
            }
            return false;
        }

        public static int DoubleToInt(this double val)
        {
            if (!(0.0 < val))
            {
                return (int)(val - 0.5);
            }
            return (int)(val + 0.5);
        }

        public static bool IsNaN(this double value)
        {
            NanUnion nanUnion = default(NanUnion);
            nanUnion.DoubleValue = value;
            ulong num = nanUnion.UintValue & 0xFFF0000000000000uL;
            ulong num2 = nanUnion.UintValue & 0xFFFFFFFFFFFFFuL;
            if (num == 9218868437227405312L || num == 18442240474082181120uL)
            {
                return num2 != 0;
            }
            return false;
        }

        public static double Round(this double value, int digits = 0)
        {
            return Math.Round(value, digits);
        }
    }
}
