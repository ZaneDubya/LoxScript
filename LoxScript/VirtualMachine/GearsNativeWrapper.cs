using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XPT.Compiling;

namespace XPT.VirtualMachine {
    class GearsNativeWrapper {
        private readonly static Dictionary<Type, GearsNativeWrapper> _Wrappers= new Dictionary<Type, GearsNativeWrapper>();

        public static GearsNativeWrapper GetWrapper(Type type) {
            if (_Wrappers.TryGetValue(type, out GearsNativeWrapper wrapper)) {
                return wrapper;
            }
            wrapper = new GearsNativeWrapper(type);
            _Wrappers[type] = wrapper;
            return wrapper;
        }

        public readonly Type WrappedType;
        private readonly Dictionary<ulong, FieldInfo> _Fields = new Dictionary<ulong, FieldInfo>();

        private GearsNativeWrapper(Type wrappedType) {
            WrappedType = wrappedType;
            BindingFlags binding = BindingFlags.Public | BindingFlags.Instance;
            FieldInfo[] fields = wrappedType.GetFields(binding);
            foreach (FieldInfo info in fields) {
                _Fields.Add(CompilerBitStr.GetBitStr(info.Name), info);
            }
            PropertyInfo[] props = wrappedType.GetProperties(binding);
            MethodInfo[] methods = wrappedType.GetMethods(binding).Where(d => !d.IsSpecialName).ToArray();
        }

        public void SetField(Gears context, object receiver, ulong name, GearsValue value) {
            if (_Fields.TryGetValue(name, out FieldInfo fieldInfo)) {
                if (value.IsNumber) {
                    if (!IsNumeric(fieldInfo.FieldType)) {
                        throw new GearsRuntimeException($"Attempted to set {WrappedType.Name}.{fieldInfo.Name} to numeric value.");
                    }
                    try {
                        fieldInfo.SetValue(receiver, Convert.ChangeType((double)value, fieldInfo.FieldType));
                        return;
                    }
                    catch (Exception e) {
                        throw new GearsRuntimeException($"Error setting {WrappedType.Name}.{fieldInfo.Name} to {(double)value}: {e.Message}");
                    }
                }
                else if (fieldInfo.FieldType == typeof(bool) && value.IsBool) {
                    fieldInfo.SetValue(receiver, value.IsTrue ? true : false);
                    return;
                }
                else if (value.IsObjPtr) {
                    GearsObj obj = value.AsObject(context);
                    if (fieldInfo.FieldType == typeof(string) && obj is GearsObjString objString) {
                        fieldInfo.SetValue(receiver, objString.Value);
                        return;
                    }
                }
            }
            throw new GearsRuntimeException($"Unsupported native conversion: Error setting {WrappedType.Name}.{fieldInfo.Name} to {value}.");
        }

        public bool TryGetField(Gears context, object receiver, ulong name, out GearsValue value) {
            if (_Fields.TryGetValue(name, out FieldInfo fieldInfo)) {
                if (IsNumeric(fieldInfo.FieldType)) {
                    double fieldValue = Convert.ToDouble(fieldInfo.GetValue(receiver));
                    value = new GearsValue(fieldValue);
                    return true;
                }
                else if (fieldInfo.FieldType == typeof(bool)) {
                    bool fieldValue = Convert.ToBoolean(fieldInfo.GetValue(receiver));
                    value = fieldValue ? GearsValue.TrueValue : GearsValue.FalseValue;
                    return true;
                }
                else if (fieldInfo.FieldType == typeof(string)) {
                    string fieldValue = fieldInfo.GetValue(receiver) as string;
                    value = GearsValue.CreateObjPtr(context.HeapAddObject(new GearsObjString(fieldValue)));
                    return true;
                }
            }
            throw new NotImplementedException();
        }

        private static bool IsNumeric(Type type) {
            switch (Type.GetTypeCode(type)) {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
