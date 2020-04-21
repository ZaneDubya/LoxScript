using System;

namespace LoxScript.VirtualMachine {
    struct GearsValue : IEquatable<GearsValue> {
        // reference for this code is https://stackoverflow.com/questions/38198739/c-sharp-creating-a-custom-double-type
        private readonly double _Value;

        public GearsValue(double value) {
            _Value = value;
        }

        public bool Equals(GearsValue other) => other._Value == _Value;

        public override string ToString() => _Value.ToString();

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
