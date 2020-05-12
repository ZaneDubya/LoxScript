using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XPT.VirtualMachine {
    [StructLayout(LayoutKind.Explicit)]
    struct GearsValue : IEquatable<GearsValue> {
        public static readonly GearsValue NilValue = new GearsValue(TAG_NIL);

        public static readonly GearsValue FalseValue = new GearsValue(TAG_FALSE);

        public static readonly GearsValue TrueValue = new GearsValue(TAG_TRUE);

        public static GearsValue CreateObjPtr(int index) => new GearsValue(TAG_OBJECTPTR | (uint)index);

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

        public bool IsObjPtr => (_AsLong & TAG_OBJECTPTR) == TAG_OBJECTPTR;

        public bool IsObjType<T>(Gears context) where T : GearsObj => IsObjPtr && AsObject(context) is T;

        // --- Return as a ... ---------------------------------------------------------------------------------------

        public bool AsBool => IsTrue ? true : false; // todo: is this correct?

        /// <summary>
        /// This is a pointer to data that lives on the Gear's heap.
        /// </summary>
        public int AsObjPtr => IsObjPtr ? (int)(_AsLong & ~(TAG_OBJECTPTR)) : -1;

        public GearsObj AsObject(Gears context) => context.HeapGetObject(AsObjPtr); // todo: fix with reference to context's heap...
        
        // --- Ctor and ToString -------------------------------------------------------------------------------------

        public GearsValue(ulong value) : this() {
            _AsLong = value;
        }

        public GearsValue(double value) : this() {
            _Value = value;
        }

        public override string ToString() => ToString(null);

        public string ToString(Gears context) {
            if (IsObjPtr) {
                if (context != null) {
                    return AsObject(context).ToString();
                }
                return $"obj@{AsObjPtr.ToString()}";
            }
            else if (IsBool) {
                return AsBool ? "true" : "false";
            }
            else if (IsNil) {
                return "nil";
            }
            else if (IsNumber) {
                return _Value.ToString();
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
            return false;
        }

        /// <summary>
        /// Implicit conversion from bool to GearsValue (no cast operator required).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GearsValue(bool value) => new GearsValue(value ? TAG_TRUE : TAG_FALSE);

        /// <summary>
        /// Implicit conversion from double to GearsValue (no cast operator required).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GearsValue(double value) => new GearsValue(value);

        /// <summary>
        /// Implicit conversion from ulong to GearsValue (no cast operator required).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GearsValue(ulong value) => new GearsValue(value);

        /// <summary>
        /// Explicit conversion from GearsValue to double (requires cast operator).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double(GearsValue value) => value._Value;

        /// <summary>
        /// Explicit conversion from GearsValue to double (requires cast operator).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ulong(GearsValue value) => value._AsLong;

        // public static GearsValue operator +(GearsValue value) => value;

        public static GearsValue operator -(GearsValue value) => -value._Value;

        public static GearsValue operator +(GearsValue a, GearsValue b) => a._Value + b._Value;

        public static GearsValue operator -(GearsValue a, GearsValue b) =>  a._Value - b._Value;

        public static GearsValue operator *(GearsValue a, GearsValue b) => a._Value * b._Value;

        public static GearsValue operator /(GearsValue a, GearsValue b) => a._Value / b._Value;

        public static GearsValue operator <(GearsValue a, GearsValue b) => a._Value < b._Value;

        public static GearsValue operator >(GearsValue a, GearsValue b) => a._Value > b._Value;
    }
}
