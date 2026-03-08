using System;
using System.Collections.Generic;
using XPT.Core.Scripting.Rules;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    internal partial class Gears { // support for native c# calling lox script functions and rules.
        /// <summary>
        /// Invokes a LoxScript function call from native c#, passing arguments.
        /// If the function call was successful, returns true, and returnValue will be the returned value from the function, if any. 
        /// If the function call was not successful, returns false, and returnValue will be an error string.
        /// </summary>
        internal bool InvokeGearsFunction(string fnName, out object returned, params object[] args) {
            if (!Globals.TryGet(fnName, out GearsValue fnValue) || !fnValue.IsObjPtr) {
                // error: no function with that name.
                returned = $"Error: no function with name '{fnName}'.";
                return false;
            }
            GearsObj fnObject = fnValue.AsObject(this);
            if (fnObject is GearsObjFunction fnFunction) {
                if (fnFunction.Arity != args.Length) {
                    // error: wrong arity.
                    returned = $"Error: called '{fnName}' with wrong arity (passed arity is '{args?.Length ?? 0}').";
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
                    int fieldValue = Convert.ToInt32(arg);
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
                    returned = $"Error: called '{fnName}' with argument of type '{argType.Name}' as parameter {i}. Gears could not interpret this argument.";
                    return false;
                }
            }
            Call(args?.Length ?? 0);
            if (!TryRun(out string error)) {
                returned = error;
                return false;
            }
            returned = LastReturnValue; // the return value
            return true;
        }

        /// <summary>
        /// Invokes a LoxScript function call from native c#, by looking up rules matching the given trigger name and rule variables, and invoking the function specified in each matching rule, passing arguments.
        /// </summary>
        internal IEnumerable<object> InvokeByRule(string triggerName, RuleVarCollection vars, params object[] args) {
            foreach (Rule rule in Chunk.Rules.GetMatching(triggerName, vars)) {
                bool success = InvokeGearsFunction(rule.InvokedFnGearsName, out object returned, args);
                if (success) {
                    yield return returned;
                }
                else {
                    throw new Exception($"Gears InvokeByRule: Failed to invoke fn {rule.InvokedFnGearsName}: {returned}.");
                }
            }
        }
    }
}
