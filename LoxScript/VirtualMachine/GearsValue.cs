using System;
using System.Runtime.InteropServices;

namespace LoxScript.VirtualMachine {
    [StructLayout(LayoutKind.Explicit)]
    struct GearsValue : IEquatable<GearsValue> {
        /// <summary>
        /// Every value that is not a number will use a special "Not a number" representation. NaN is defined
        /// by having 13 bits (defined by IEE 754) set. No valid double will have these bits set. The bits are:
        /// All 11 exponent bits (NaN) + bit 51 (quiet NaN) + bit 50 (QNan FP Indefinite).
        /// </summary>
        private const ulong QNAN = 0x7FFC000000000000;
        private const ulong SIGN_BIT = 0x8000000000000000;
        private const ulong TAG_NIL = 0x0000000000000001 | QNAN;
        private const ulong TAG_FALSE = 0x0000000000000002 | QNAN;
        private const ulong TAG_TRUE = 0x0000000000000003 | QNAN;
        private const ulong TAG_OBJECTPTR = QNAN | SIGN_BIT;

        // 64-bit double-precision IEEE floating-point number.
        // 52 mantissa bits, 11 exponent bits, 1 sign bit.
        [FieldOffset(0)]
        private readonly double _Value;
        [FieldOffset(0)]
        private readonly ulong _AsLong;

        // --- Is this a ... -----------------------------------------------------------------------------------------

        public bool IsNumber => (_AsLong & QNAN) != QNAN;

        public bool IsNil => _AsLong == TAG_NIL;

        public bool IsFalse => _AsLong == TAG_FALSE;

        public bool IsTrue => _AsLong == TAG_TRUE;

        public bool IsBool => (_AsLong & TAG_FALSE) == TAG_FALSE;

        public bool IsObjectPtr => (_AsLong & TAG_OBJECTPTR) == TAG_OBJECTPTR;

        // --- Return as a ... ---------------------------------------------------------------------------------------

        private bool AsBool => IsTrue ? true : false; // todo: is this correct?

        private int AsObjectPtr => IsObjectPtr ? (int)(_AsLong & ~(TAG_OBJECTPTR)) : -1;

        // --- Ctor and ToString -------------------------------------------------------------------------------------

        public GearsValue(double value) : this() {
            _Value = value;
        }

        public override string ToString() {
            if (IsBool) {
                return AsBool ? "true" : "false";
            }
            else if (IsNil) {
                return "nil";
            }
            else if (IsNumber) {
                return _Value.ToString();
            }
            else if (IsObjectPtr) {
                return AsObjectPtr.ToString();
            }
            else {
                throw new Exception("Unknown GearsValue type!");
            }
        }

        // --- Equality and Operations -------------------------------------------------------------------------------

        public bool Equals(GearsValue other) {
            if (IsNumber && other.IsNumber) {
                return _Value == other._Value;
            }
            return _AsLong == other._AsLong;
        }

        /// <summary>
        /// Implicit conversion from double to GearsValue (no cast operator required).
        /// </summary>
        public static implicit operator GearsValue(double value) => new GearsValue(value);

        /// <summary>
        /// Explicit conversion from MoneyAmount to double (requires cast operator).
        /// </summary>
        public static explicit operator double(GearsValue value) => value._Value;

        // public static GearsValue operator +(GearsValue value) => value;

        public static GearsValue operator -(GearsValue value) => -value._Value;

        public static GearsValue operator +(GearsValue a, GearsValue b) => a._Value + b._Value;

        public static GearsValue operator -(GearsValue a, GearsValue b) =>  a._Value - b._Value;

        public static GearsValue operator *(GearsValue a, GearsValue b) => a._Value * b._Value;

        public static GearsValue operator /(GearsValue a, GearsValue b) => a._Value / b._Value;
    }
}
