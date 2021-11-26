using System.Collections.Generic;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class Rule {
        internal readonly ulong Trigger;
        internal readonly ulong Result;
        internal readonly RuleCondition[] Conditions;

        public Rule(ulong trigger, ulong pointer, RuleCondition[] ruleConditions) {
            Trigger = trigger;
            Result = pointer;
            Conditions = ruleConditions;
        }

        internal bool IsTrue(ulong trigger, RuleInvocationContext context) {
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
            writer.Write(Trigger);
            writer.Write(Result);
            writer.Write7BitInt(Conditions.Length);
            for (int i = 0; i < Conditions.Length; i++) {
                Conditions[i].Serialize(writer);
            }
        }

        internal static Rule Deserialize(IReader reader) {
            ulong trigger = (ulong)reader.ReadLong();
            ulong result = (ulong)reader.ReadLong();
            int count = reader.Read7BitInt();
            List<RuleCondition> cs = new List<RuleCondition>(count);
            for (int i = 0; i < count; i++) {
                cs.Add(RuleCondition.Deserialize(reader));
            }
            return new Rule(trigger, result, cs.ToArray());
        }

        public override string ToString() => $"[{BitString.GetBitStr(Trigger)} ...] => {Result}()";
    }
}
