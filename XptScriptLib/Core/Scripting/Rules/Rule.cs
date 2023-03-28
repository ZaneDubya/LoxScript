using System.Collections.Generic;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// A Rule is the basic unit of logic in the RuleSystem. When the named Trigger is fired, the Rule will be
    /// evaluated, and if all of the conditions are true, the InvokedFnName will be invoked.
    /// </summary>
    class Rule {
        internal readonly string Trigger;
        internal readonly string InvokedFnName;
        internal readonly RuleCondition[] Conditions;

        private readonly RuleInvocationDelegateHosted _OnInvokeNative; // use this for c# code
        private readonly RuleInvocationDelegateNative _OnInvokeHosted; // use this for loxscript code

        public Rule(string trigger, string fnName, RuleCondition[] conditions) {
            Trigger = trigger;
            InvokedFnName = fnName;
            Conditions = conditions;
            _OnInvokeNative = null;
            _OnInvokeHosted = null;
        }

        public Rule(string trigger, string fnName, RuleCondition[] conditions, RuleInvocationDelegateHosted onInvoke) {
            Trigger = trigger;
            InvokedFnName = fnName;
            Conditions = conditions;
            _OnInvokeNative = onInvoke;
            _OnInvokeHosted = null;
        }

        public Rule(string trigger, string fnName, RuleCondition[] conditions, RuleInvocationDelegateNative onInvoke) {
            Trigger = trigger;
            InvokedFnName = fnName;
            Conditions = conditions;
            _OnInvokeNative = null;
            _OnInvokeHosted = onInvoke;
        }

        internal bool Match(string trigger, VarCollection context) {
            if (Trigger != trigger) {
                return false;
            }
            foreach (RuleCondition condition in Conditions) {
                if (!condition.IsTrue(context)) {
                    return false;
                }
            }
            return true;
        }

        internal void Invoke(VarCollection args) {
            if (_OnInvokeNative != null) {
                _OnInvokeNative.Invoke(InvokedFnName, args);
            }
            else {
                _OnInvokeHosted.Invoke(args);
            }
        }

        internal void Serialize(IWriter writer) {
            writer.WriteAsciiPrefix(Trigger);
            writer.WriteAsciiPrefix(InvokedFnName);
            writer.Write7BitInt(Conditions.Length);
            for (int i = 0; i < Conditions.Length; i++) {
                Conditions[i].Serialize(writer);
            }
        }

        /// <summary>
        /// Deserializes a rule from binary data. Does not make rules invocable.
        /// </summary>
        internal static Rule Deserialize(IReader reader) {
            string trigger = reader.ReadAsciiPrefix();
            string invokeFnName = reader.ReadAsciiPrefix();
            int count = reader.Read7BitInt();
            List<RuleCondition> cs = new List<RuleCondition>(count);
            for (int i = 0; i < count; i++) {
                cs.Add(RuleCondition.Deserialize(reader));
            }
            return new Rule(trigger, invokeFnName, cs.ToArray());
        }

        internal Rule CreateCopyWithHostedDelegate(RuleInvocationDelegateHosted fn) {
            return new Rule(Trigger, InvokedFnName, Conditions, fn);
        }

        public override string ToString() => $"[{Trigger} ...] => {InvokedFnName}(context)";
    }
}
