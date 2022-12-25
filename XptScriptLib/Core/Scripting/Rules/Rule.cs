using System.Collections.Generic;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class Rule {
        internal readonly string Trigger;
        internal readonly string ResultFnName;
        internal readonly RuleCondition[] Conditions;

        public Rule(string trigger, string resultFnName, RuleCondition[] ruleConditions) {
            Trigger = trigger;
            ResultFnName = resultFnName;
            Conditions = ruleConditions;
        }

        internal bool IsTrue(string trigger, RuleInvocationContext context) {
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

        internal void Serialize(IWriter writer) {
            writer.WriteAsciiPrefix(Trigger);
            writer.WriteAsciiPrefix(ResultFnName);
            writer.Write7BitInt(Conditions.Length);
            for (int i = 0; i < Conditions.Length; i++) {
                Conditions[i].Serialize(writer);
            }
        }

        internal static Rule Deserialize(IReader reader) {
            string trigger = reader.ReadAsciiPrefix();
            string result = reader.ReadAsciiPrefix();
            int count = reader.Read7BitInt();
            List<RuleCondition> cs = new List<RuleCondition>(count);
            for (int i = 0; i < count; i++) {
                cs.Add(RuleCondition.Deserialize(reader));
            }
            return new Rule(trigger, result, cs.ToArray());
        }

        public override string ToString() => $"[{Trigger} ...] => {ResultFnName}()";
    }
}
