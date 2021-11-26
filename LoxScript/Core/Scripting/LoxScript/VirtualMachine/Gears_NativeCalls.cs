using System;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    internal partial class Gears { // support for native c# calling lox script functions.

        /// <summary>
        /// Calls a LoxScript function from native code, passing arguments.
        /// If the function call was successful, returns true, and returnValue will be the returned value from the function, if any. 
        /// If the function call was not successful, returns false, and returnValue will be an error string.
        /// </summary>
        internal bool CallGearsFunction(string fnName, out object returned, params object[] args) {
            ulong name = BitString.GetBitStr(fnName);
            return CallGearsFunction(name, out returned, args);
        }

        internal bool CallGearsFunction(ulong name, out object returned, params object[] args) {
            if (!Globals.TryGet(name, out GearsValue fnValue) || !fnValue.IsObjPtr) {
                // error: no function with that name.
                returned = $"Error: no function with name '{BitString.GetBitStr(name)}'.";
                return false;
            }
            GearsObj fnObject = fnValue.AsObject(this);
            if (fnObject is GearsObjFunction fnFunction) {
                if (fnFunction.Arity != args.Length) {
                    // error: wrong arity.
                    returned = $"Error: called '{BitString.GetBitStr(name)}' with wrong arity (passed arity is '{args?.Length ?? 0}').";
                    return false;
                }
            }
            Push(fnValue);
            for (int i = 0; i < (args?.Length ?? 0); i++) {
                object arg = args[i];
                Type argType = arg?.GetType() ?? null;
                if (arg == null) {
                    Push(GearsValue.NilValue);
                }
                else if (GearsNativeWrapper.IsNumeric(argType)) {
                    double fieldValue = Convert.ToDouble(arg);
                    Push(new GearsValue(fieldValue));
                }
                else if (argType == typeof(bool)) {
                    bool fieldValue = Convert.ToBoolean(arg);
                    Push(fieldValue ? GearsValue.TrueValue : GearsValue.FalseValue);
                }
                else if (argType == typeof(string)) {
                    string fieldValue = Convert.ToString(arg);
                    if (fieldValue == null) {
                        Push(GearsValue.NilValue);
                    }
                    else {
                        Push(GearsValue.CreateObjPtr(HeapAddObject(new GearsObjString(fieldValue))));
                    }
                }
                else if (argType.IsSubclassOf(typeof(object))) {
                    if (arg == null) {
                        Push(GearsValue.NilValue);
                    }
                    else {
                        Push(GearsValue.CreateObjPtr(HeapAddObject(new GearsObjInstanceNative(this, arg))));
                    }
                }
                else {
                    // error: could not pass arg of this type
                    returned = $"Error: called '{BitString.GetBitStr(name)}' with argument of type '{argType.Name}' as parameter {i}. Gears could not interpret this argument.";
                    return false;
                }
            }
            Call(args?.Length ?? 0);
            Run();
            returned = LastReturnValue; // the return value
            // todo: process return value?
            return true;
        }
    }
}
