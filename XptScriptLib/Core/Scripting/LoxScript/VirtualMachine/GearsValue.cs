#if NET_4_5
using System.Runtime.CompilerServices;
#endif
using System;
using System.Runtime.InteropServices;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// GearsValue is the type used to represent values in the VM. It is a 32-bit signed integer with special values
    /// assigned to certain bit combinations. The range of a GearsValue number is -2,147,483,648 to +1,073,741,823.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct GearsValue : IEquatable<GearsValue> {

        // A GearsValue is a 32-bit signed integer with special value assigned to certain bits.
        // bit 31 (0x8... ....) is negative (as normal).
        // bit 30 (0x4... ....) represents "not a number" values. There is only one NaN value, nil (0x40000001).
        // bit 29 (0x2... ....) represents pointers to objects.
        // The range of a GearsValue as a number is -2,147,483,648 to +1,073,741,823

        public static readonly GearsValue NilValue = new GearsValue(TAG_NIL);

        public static readonly GearsValue FalseValue = new GearsValue(0);

        /// <summary>
        /// An example value that is considered true. Do not compare against this value to determine truthiness. 
        /// Instead, use the IsTrue property.
        /// </summary>
        public static readonly GearsValue TrueValue = new GearsValue(1);

        public static GearsValue CreateObjPtr(int index) => new GearsValue(TAG_OBJPTR | index);

        /// <summary>
        /// Every value that is not a number will have a special value: the 31st bit will not be set, and the 30th bit
        /// will be set. No value numeric GearsValue will have this bit combination. This mask is these two bits.
        /// </summary>
        private const uint MASK_NAN = 0xC0000000;
        private const uint MASK_NAN_AND_OBJPTR = 0xE0000000;

        /// <summary>
        /// Every value that is not a number will use a special "Not a number" representation. NaN is the 30th bit set
        /// and not a negative number. (value & MASK_NAN) == BIT_NAN represents this value.
        /// </summary>
        private const int BIT_NAN = 0x40000000;
        private const int BIT_OBJPTR = 0x20000000;

        private const int TAG_OBJPTR = BIT_NAN | BIT_OBJPTR;
        private const int TAG_NIL = BIT_NAN | 0x00000001;

        [FieldOffset(0)]
        private readonly int _Value;

        // --- Is this a ... -----------------------------------------------------------------------------------------

        public bool IsNumber => ((uint)_Value & MASK_NAN) != BIT_NAN;

        public bool IsNil => _Value == TAG_NIL;

        /// <summary>
        /// If the value is 0, it is considered false. Any other value is considered true.
        /// </summary>
        public bool IsFalse => _Value == 0;

        /// <summary>
        /// Any non-zero number, including a negative number, is considered true. Note that NaN values are considered true.
        /// </summary>
        public bool IsTrue => _Value != 0;

        public bool IsObjPtr => ((uint)_Value & MASK_NAN_AND_OBJPTR) == TAG_OBJPTR;

        public bool IsObjType<T>(Gears context) where T : GearsObj => IsObjPtr && AsObject(context) is T;

        // --- Return as a ... ---------------------------------------------------------------------------------------

        /// <summary>
        /// A value is true if it is not 0x00000000.
        /// </summary>
        public bool AsBool => IsTrue;

        /// <summary>
        /// This is a pointer to data that lives on the Gear's heap.
        /// </summary>
        public int AsObjPtr => IsObjPtr ? _Value & ~TAG_OBJPTR : -1;

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
