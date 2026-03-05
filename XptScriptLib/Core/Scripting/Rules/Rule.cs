using System;
using System.Collections.Generic;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// A Rule is the basic unit of logic in the RuleSystem. When the named Trigger is fired, the Rule will be
    /// evaluated, and if all of the conditions are true, the InvokedFnName will be invoked.
    /// </summary>
    class Rule {
        internal readonly string Trigger;
        internal readonly string InvokedFnGearsName;
        internal readonly RuleCondition[] Conditions;

        public Rule(string trigger, string invokedGearsFunctionName, RuleCondition[] conditions) {
            Trigger = trigger;
            InvokedFnGearsName = invokedGearsFunctionName;
            Conditions = conditions;
        }

        public override string ToString() => $"[{Trigger} ...] => {InvokedFnGearsName}(context)";

        // === match and invoke =======================================================================================
        // ============================================================================================================

        internal bool Match(string trigger, RuleVarCollection vars) {
            if (Trigger != trigger) {
                return false;
            }
            foreach (RuleCondition condition in Conditions) {
                if (!condition.IsTrue(vars)) {
                    return false;
                }
            }
            return true;
        }

        // === serialization - use this only for Rules attached to GearsChunk =========================================
        // ============================================================================================================

        internal void Serialize(IWriter writer) {
            writer.WriteAsciiPrefix(Trigger);
            writer.WriteAsciiPrefix(InvokedFnGearsName);
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
            string invokedFnName = reader.ReadAsciiPrefix();
            int count = reader.Read7BitInt();
            List<RuleCondition> cs = new List<RuleCondition>(count);
            for (int i = 0; i < count; i++) {
                cs.Add(RuleCondition.Deserialize(reader));
            }
            return new Rule(trigger, invokedFnName, cs.ToArray());
        }
    }
}
