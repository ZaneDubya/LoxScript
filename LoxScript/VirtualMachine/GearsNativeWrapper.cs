using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XPT.Compiling;

namespace XPT.VirtualMachine {
    class GearsNativeWrapper {
        private readonly static Dictionary<Type, GearsNativeWrapper> _Wrappers;

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

        public void SetField(object obj, ulong name, GearsValue value) {
            throw new NotImplementedException();
        }

        public bool TryGetField(object obj, ulong name, out GearsValue value) {
            throw new NotImplementedException(); 
        }
    }
}
