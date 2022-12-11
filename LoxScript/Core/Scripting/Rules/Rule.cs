using System.Collections.Generic;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class Rule {
        internal readonly long Trigger;
        internal readonly long Result;
        internal readonly RuleCondition[] Conditions;

        public Rule(long trigger, long pointer, RuleCondition[] ruleConditions) {
            Trigger = trigger;
            Result = pointer;
            Conditions = ruleConditions;
        }

        internal bool IsTrue(long trigger, RuleInvocationContext context) {
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
            long trigger = (long)reader.ReadLong();
            long result = (long)reader.ReadLong();
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
