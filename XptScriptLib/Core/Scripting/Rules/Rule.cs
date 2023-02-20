using System.Collections.Generic;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class Rule {
        internal readonly string Trigger;
        internal readonly string InvokedFnName;
        internal readonly RuleCondition[] Conditions;
        private readonly RuleInvocationWithName _OnInvokeWithName;
        private readonly RuleInvocationWithoutName _OnInvokeWithoutName;

        public Rule(string trigger, string fnName, RuleCondition[] conditions) {
            Trigger = trigger;
            InvokedFnName = fnName;
            Conditions = conditions;
            _OnInvokeWithName = null;
            _OnInvokeWithoutName = null;
        }

        public Rule(string trigger, string fnName, RuleCondition[] conditions, RuleInvocationWithName onInvoke) {
            Trigger = trigger;
            InvokedFnName = fnName;
            Conditions = conditions;
            _OnInvokeWithName = onInvoke;
            _OnInvokeWithoutName = null;
        }

        public Rule(string trigger, string fnName, RuleCondition[] conditions, RuleInvocationWithoutName onInvoke) {
            Trigger = trigger;
            InvokedFnName = fnName;
            Conditions = conditions;
            _OnInvokeWithName = null;
            _OnInvokeWithoutName = onInvoke;
        }

        internal bool Match(string trigger, RuleInvocationContext context) {
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

        internal object Invoke(params object[] args) {
            return (_OnInvokeWithName != null) ? _OnInvokeWithName.Invoke(InvokedFnName, args) : _OnInvokeWithoutName.Invoke(args);
        }

        internal void Serialize(IWriter writer) {
            writer.WriteAsciiPrefix(Trigger);
            writer.WriteAsciiPrefix(InvokedFnName);
            writer.Write7BitInt(Conditions.Length);
            for (int i = 0; i < Conditions.Length; i++) {
                Conditions[i].Serialize(writer);
            }
        }

        internal static Rule Deserialize(IReader reader, RuleInvocationWithName onInvoke) {
            string trigger = reader.ReadAsciiPrefix();
            string invokeFnName = reader.ReadAsciiPrefix();
            int count = reader.Read7BitInt();
            List<RuleCondition> cs = new List<RuleCondition>(count);
            for (int i = 0; i < count; i++) {
                cs.Add(RuleCondition.Deserialize(reader));
            }
            return new Rule(trigger, invokeFnName, cs.ToArray(), onInvoke);
        }

        public override string ToString() => $"[{Trigger} ...] => {InvokedFnName}(context)";
    }
}
