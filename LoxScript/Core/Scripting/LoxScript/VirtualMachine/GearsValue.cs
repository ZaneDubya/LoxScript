#if NET_4_5
using System.Runtime.CompilerServices;
#endif
using System;
using System.Runtime.InteropServices;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    [StructLayout(LayoutKind.Explicit)]
    internal struct GearsValue : IEquatable<GearsValue> {
        public static readonly GearsValue NilValue = new GearsValue(TAG_NIL);

        public static readonly GearsValue FalseValue = new GearsValue(TAG_FALSE);

        public static readonly GearsValue TrueValue = new GearsValue(TAG_TRUE);

        public static GearsValue CreateObjPtr(int index) => new GearsValue(TAG_OBJECTPTR | (int)index);

        /// <summary>
        /// Every value that is not a number will use a special "Not a number" representation. NaN is defined
        /// by having the 63rd bit set. No valid numberic GearsValue will have this bit set.
        /// </summary>
        private const long QNAN = 0x4000000000000000;
        private const long SIGN_BIT = unchecked((long)0x8000000000000000);
        private const long TAG_NIL = 0x0000000000000001 | QNAN;
        private const long TAG_FALSE = 0x0000000000000002 | QNAN;
        private const long TAG_TRUE = 0x0000000000000003 | QNAN;
        private const long TAG_OBJECTPTR = QNAN | 0x2000000000000000;

        [FieldOffset(0)]
        private readonly long _AsLong;

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

        public GearsValue(long value) : this() {
            _AsLong = value;
        }

        public override string ToString() => ToString(null);

        public string ToString(Gears context) {
            if (IsObjPtr) {
                if (context != null) {
                    return AsObject(context).ToString();
                }
                return $"objPtr(@{AsObjPtr})";
            }
            else if (IsBool) {
                return AsBool ? "true" : "false";
            }
            else if (IsNil) {
                return "nil";
            }
            else if (IsNumber) {
                return _AsLong.ToString();
            }
            else {
                throw new Exception("Unknown GearsValue type!");
            }
        }

        // --- Equality and Operations -------------------------------------------------------------------------------

        public bool Equals(GearsValue other) {
            if (IsNumber && other.IsNumber) {
                return _AsLong == other._AsLong;
            }
            return false;
        }

        /// <summary>
        /// Implicit conversion from bool to GearsValue (no cast operator required).
        /// </summary>
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static implicit operator GearsValue(bool value) => new GearsValue(value ? TAG_TRUE : TAG_FALSE);

        /// <summary>
        /// Implicit conversion from long to GearsValue (no cast operator required).
        /// </summary>
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static implicit operator GearsValue(long value) => new GearsValue(value);

        /// <summary>
        /// Explicit conversion from GearsValue to long (requires cast operator).
        /// </summary>
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static explicit operator long(GearsValue value) => value._AsLong;

        // public static GearsValue operator +(GearsValue value) => value;

        public static GearsValue operator -(GearsValue value) => -value._AsLong;

        public static GearsValue operator +(GearsValue a, GearsValue b) => a._AsLong + b._AsLong;

        public static GearsValue operator -(GearsValue a, GearsValue b) =>  a._AsLong - b._AsLong;

        public static GearsValue operator *(GearsValue a, GearsValue b) => a._AsLong * b._AsLong;

        public static GearsValue operator /(GearsValue a, GearsValue b) => a._AsLong / b._AsLong;

        public static GearsValue operator <(GearsValue a, GearsValue b) => a._AsLong < b._AsLong;

        public static GearsValue operator >(GearsValue a, GearsValue b) => a._AsLong > b._AsLong;
    }
}
