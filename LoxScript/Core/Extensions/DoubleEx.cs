using System;

namespace XPT.Core.Extensions {
    static class DoubleEx {
        public static ulong ConvertToULong(this double value) {
            return BitConverter.ToUInt64(BitConverter.GetBytes(value), 0);
        }

        public static double ConvertToDouble(this ulong value) {
            return BitConverter.ToDouble(BitConverter.GetBytes(value), 0);
        }
    }
}
