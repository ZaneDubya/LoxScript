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
                    double number = (double)value;
                    if (fieldInfo.FieldType == typeof(double)) {
                        fieldInfo.SetValue(receiver, number);
                        return;
                    }
                    else if (fieldInfo.FieldType == typeof(int)) {
                        fieldInfo.SetValue(receiver, Convert.ToInt32(number));
                        return;
                    }
                }
            }
            throw new NotImplementedException();
        }

        public bool TryGetField(Gears context, object receiver, ulong name, out GearsValue value) {
            if (_Fields.TryGetValue(name, out FieldInfo fieldInfo)) {
                if (fieldInfo.FieldType == typeof(double) || fieldInfo.FieldType == typeof(int)) {
                    double fieldValue =Convert.ToDouble(fieldInfo.GetValue(receiver));
                    value = new GearsValue(fieldValue);
                    return true;
                }
            }
            throw new NotImplementedException(); 
        }
    }
}
