using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XPT.Core.Scripting.Compiling;

namespace XPT.Core.Scripting.VirtualMachine {
    class GearsNativeWrapper {
        private readonly static Dictionary<Type, GearsNativeWrapper> _Wrappers = new Dictionary<Type, GearsNativeWrapper>();

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
        private readonly Dictionary<ulong, MethodInfo> _Methods = new Dictionary<ulong, MethodInfo>();
        private readonly Dictionary<ulong, PropertyInfo> _Properties = new Dictionary<ulong, PropertyInfo>();

        private GearsNativeWrapper(Type wrappedType) {
            WrappedType = wrappedType;
            BindingFlags binding = BindingFlags.Public | BindingFlags.Instance;
            FieldInfo[] fields = wrappedType.GetFields(binding);
            foreach (FieldInfo info in fields) {
                _Fields.Add(CompilerBitStr.GetBitStr(info.Name), info);
            }
            MethodInfo[] methods = wrappedType.GetMethods(binding).Where(d => !d.IsSpecialName).ToArray();
            foreach (MethodInfo info in methods) {
                _Methods.Add(CompilerBitStr.GetBitStr(info.Name), info);
            }
            PropertyInfo[] properties = wrappedType.GetProperties(binding);
            foreach (PropertyInfo info in properties) {
                _Properties.Add(CompilerBitStr.GetBitStr(info.Name), info);
            }
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
                else if (value.IsNil && fieldInfo.FieldType == typeof(string)) {
                    fieldInfo.SetValue(receiver, null);
                    return;
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
            else if (_Properties.TryGetValue(name, out PropertyInfo propertyInfo)) {
                if (!propertyInfo.SetMethod.IsPublic) {
                    throw new GearsRuntimeException($"Unsupported reference: Native class {WrappedType.Name} does not have a public set method for '{CompilerBitStr.GetBitStr(name)}'.");
                }
                if (value.IsNumber) {
                    if (!IsNumeric(propertyInfo.PropertyType)) {
                        throw new GearsRuntimeException($"Attempted to set {WrappedType.Name}.{propertyInfo.Name} to numeric value.");
                    }
                    try {
                        propertyInfo.SetValue(receiver, Convert.ChangeType((double)value, propertyInfo.PropertyType));
                        return;
                    }
                    catch (Exception e) {
                        throw new GearsRuntimeException($"Error setting {WrappedType.Name}.{propertyInfo.Name} to {(double)value}: {e.Message}");
                    }
                }
                else if (value.IsNil && propertyInfo.PropertyType == typeof(string)) {
                    propertyInfo.SetValue(receiver, null);
                    return;
                }
                else if (propertyInfo.PropertyType == typeof(bool) && value.IsBool) {
                    propertyInfo.SetValue(receiver, value.IsTrue ? true : false);
                    return;
                }
                else if (value.IsObjPtr) {
                    GearsObj obj = value.AsObject(context);
                    if (propertyInfo.PropertyType == typeof(string) && obj is GearsObjString objString) {
                        propertyInfo.SetValue(receiver, objString.Value);
                        return;
                    }
                }
            }
            throw new GearsRuntimeException($"Unsupported native conversion: Error setting {WrappedType.Name}.{CompilerBitStr.GetBitStr(name)} to {value}.");
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
                    if (!(fieldInfo.GetValue(receiver) is string fieldValue)) {
                        value = GearsValue.NilValue;
                    }
                    else {
                        value = GearsValue.CreateObjPtr(context.HeapAddObject(new GearsObjString(fieldValue)));
                    }
                    return true;
                }
            }
            else if (_Methods.TryGetValue(name, out MethodInfo methodInfo)) {
                value = GearsValue.CreateObjPtr(context.HeapAddObject(
                    new GearsObjFunctionNative(
                        methodInfo.Name, methodInfo.GetParameters().Length, (GearsValue[] args) => CreateNativeClosure(context, receiver, methodInfo, args))));
                return true;
            }
            else if (_Properties.TryGetValue(name, out PropertyInfo propertyInfo)) {
                if (!propertyInfo.GetMethod.IsPublic) {
                    throw new GearsRuntimeException($"Unsupported reference: Native class {WrappedType.Name} does not have a public get method for '{CompilerBitStr.GetBitStr(name)}'.");
                }
                if (IsNumeric(propertyInfo.PropertyType)) {
                    double fieldValue = Convert.ToDouble(propertyInfo.GetValue(receiver));
                    value = new GearsValue(fieldValue);
                    return true;
                }
                else if (propertyInfo.PropertyType == typeof(bool)) {
                    bool fieldValue = Convert.ToBoolean(propertyInfo.GetValue(receiver));
                    value = fieldValue ? GearsValue.TrueValue : GearsValue.FalseValue;
                    return true;
                }
                else if (propertyInfo.PropertyType == typeof(string)) {
                    if (!(propertyInfo.GetValue(receiver) is string fieldValue)) {
                        value = GearsValue.NilValue;
                    }
                    else {
                        value = GearsValue.CreateObjPtr(context.HeapAddObject(new GearsObjString(fieldValue)));
                    }
                    return true;
                }
            }
            throw new GearsRuntimeException($"Unsupported reference: Native class {WrappedType.GetType().Name} does not have a public field named '{CompilerBitStr.GetBitStr(name)}'.");
        }

        private static GearsValue CreateNativeClosure(Gears context, object receiver, MethodInfo methodInfo, GearsValue[] args) {
            object[] parameters = new object[args.Length];
            ParameterInfo[] paramInfo = methodInfo.GetParameters();
            if (paramInfo.Length != parameters.Length) {
                throw new GearsRuntimeException($"NativeWrapper error: {receiver.GetType().Name}.{methodInfo.Name} param info count did not match passed param count.");
            }
            for (int i = 0; i < args.Length; i++) {
                GearsValue value = args[i];
                ParameterInfo info = paramInfo[i];
                if (value.IsNumber) {
                    if (!IsNumeric(info.ParameterType)) {
                        throw new GearsRuntimeException($"Attempted to set {receiver.GetType().Name}.{info.Name} to numeric value.");
                    }
                    try {
                        parameters[i] = Convert.ChangeType((double)value, info.ParameterType);
                        break;
                    }
                    catch (Exception e) {
                        throw new GearsRuntimeException($"Error setting {receiver.GetType().Name}.{info.Name} to {(double)value}: {e.Message}");
                    }
                }
                else if (value.IsNil && info.ParameterType == typeof(string)) {
                    parameters[i] = null;
                    break;
                }
                else if (value.IsBool && info.ParameterType == typeof(bool)) {
                    parameters[i] = value.IsTrue ? true : false;
                    break;
                }
                else if (value.IsObjPtr) {
                    GearsObj obj = value.AsObject(context);
                    if (info.ParameterType == typeof(string) && obj is GearsObjString objString) {
                        parameters[i] = objString.Value;
                        break;
                    }
                }
                throw new GearsRuntimeException($"Unsupported native conversion: Error setting parameter {i} for {receiver.GetType().Name}.{info.Name} to {value}.");
            }
            object returnValue = methodInfo.Invoke(receiver, parameters);
            if (methodInfo.ReturnType == typeof(void)) {
                return GearsValue.NilValue;
            }
            else if (IsNumeric(methodInfo.ReturnType)) {
                return new GearsValue(Convert.ToDouble(returnValue));
            }
            else if (methodInfo.ReturnType == typeof(bool)) {
                return Convert.ToBoolean(returnValue) ? GearsValue.TrueValue : GearsValue.FalseValue;
            }
            else if (methodInfo.ReturnType == typeof(string)) {
                if (returnValue == null) {
                    return GearsValue.NilValue;
                }
                else {
                    return GearsValue.CreateObjPtr(context.HeapAddObject(new GearsObjString(Convert.ToString(returnValue))));
                }
            }
            else {
                throw new GearsRuntimeException($"Unsupported native return type: {receiver.GetType().Name}.{methodInfo.Name} cannot return type of {methodInfo.ReturnType.Name}.");
            }
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
