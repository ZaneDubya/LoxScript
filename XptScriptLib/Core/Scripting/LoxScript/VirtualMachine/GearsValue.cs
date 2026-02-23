#if NET_4_5
using System.Runtime.CompilerServices;
#endif
using System;
using System.Runtime.InteropServices;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// GearsValue is the type used to represent values in the VM. It is a 32-bit signed integer with special values
    /// assigned to certain bit combinations. GearsValue as a number has a range of -2,147,483,648 to +1,073,741,823.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct GearsValue : IEquatable<GearsValue> {

        // A GearsValue is a 32-bit signed integer with special value assigned to certain bits.
        // bit 31 (0x8... ....) indicates negative numbers (as normal).
        // bit 30 (0x4... ....) represents "not a number" values. Nil (0x7FFFFFFF) is NaN. Other NaN numbers are
        //                      pointers to objects (0x40000000 to 0x7FFFFFFE).
        //                      0x00000000 to 0x3FFFFFFF represents positive numbers (as normal).
        // The range of a GearsValue as a number is -2,147,483,648 to +1,073,741,823

        /// <summary>
        /// An example value that is considered true. Do not compare against this value to determine truthiness. 
        /// Instead, use the IsTrue property.
        /// </summary>
        public static readonly GearsValue TrueValue = new GearsValue(1);
        public static readonly GearsValue FalseValue = new GearsValue(0);
        public static readonly GearsValue NilValue = new GearsValue(TAG_NIL);

        public static GearsValue CreateObjPtr(int index) => new GearsValue(BIT_NAN | index);

        private const uint MASK_NAN = 0xC0000000; // NaN values have bit 31 not set, and bit 30 set. We check these two bits with this mask.
        private const int BIT_NAN = 0x40000000; // NaN values has the 30th bit set. This is that bit.
        private const int TAG_NIL = BIT_NAN | 0x3FFFFFFF; // Nil has bits 0-30 set, bit 31 is not set.

        [FieldOffset(0)]
        private readonly int _Value;

        // --- Is this a ... -----------------------------------------------------------------------------------------

        public bool IsNumber => ((uint)_Value & MASK_NAN) != BIT_NAN;

        public bool IsObjPtr => (((uint)_Value & MASK_NAN) == BIT_NAN) && !IsNil;

        public bool IsNil => _Value == TAG_NIL;

        /// <summary>
        /// If the value is 0, it is considered false. Any other value is considered true.
        /// </summary>
        public bool IsFalse => _Value == 0;

        /// <summary>
        /// Any non-zero number, including a negative number, is considered true. Note that NaN values are considered true.
        /// </summary>
        public bool IsTrue => _Value != 0;

        public bool IsObjType<T>(Gears context) where T : GearsObj => IsObjPtr && AsObject(context) is T;

        // --- Return as a ... ---------------------------------------------------------------------------------------

        /// <summary>
        /// A value is true if it is not 0x00000000.
        /// </summary>
        public bool AsBool => IsTrue;

        /// <summary>
        /// This is a pointer to data that lives on the Gear's heap.
        /// </summary>
        public int AsObjPtr => IsObjPtr ? _Value & ~BIT_NAN : -1;

        public GearsObj AsObject(Gears context) => context.HeapGetObject(AsObjPtr); // todo: fix with reference to context's heap...

        // --- Ctor and ToString -------------------------------------------------------------------------------------

        public GearsValue(int value) : this() {
            _Value = value;
        }

        public override string ToString() => ToString(null);

        public string ToString(Gears context) {
            if (IsObjPtr) {
                if (context != null) {
                    return AsObject(context).ToString();
                }
                return $"objPtr(@{AsObjPtr})";
            }
            /*else if (IsBool) { <--- removed because there are no bools in Gears; 0 and !0 are used instead.
                return AsBool ? "true" : "false";
            }*/
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
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static implicit operator GearsValue(bool value) => new GearsValue(value ? 1 : 0);

        /// <summary>
        /// Implicit conversion from int to GearsValue (no cast operator required).
        /// </summary>
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static implicit operator GearsValue(int value) => new GearsValue(value);

        /// <summary>
        /// Explicit conversion from GearsValue to int (requires cast operator).
        /// </summary>
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static explicit operator int(GearsValue value) => value._Value;

        // public static GearsValue operator +(GearsValue value) => value;

        public static GearsValue operator -(GearsValue value) => -value._Value;

        public static GearsValue operator ~(GearsValue value) => ~value._Value;

        public static GearsValue operator +(GearsValue a, GearsValue b) => a._Value + b._Value;

        public static GearsValue operator -(GearsValue a, GearsValue b) => a._Value - b._Value;

        public static GearsValue operator *(GearsValue a, GearsValue b) => a._Value * b._Value;

        public static GearsValue operator /(GearsValue a, GearsValue b) => a._Value / b._Value;

        public static GearsValue operator %(GearsValue a, GearsValue b) => a._Value % b._Value;

        public static GearsValue operator <(GearsValue a, GearsValue b) => a._Value < b._Value;

        public static GearsValue operator >(GearsValue a, GearsValue b) => a._Value > b._Value;
    }
}
