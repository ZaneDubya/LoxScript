using XPT.Core.Extensions;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class RuleCondition {
        internal readonly string VarName;
        internal readonly int Min;
        internal readonly int Max;

        private RuleCondition(string varName, int min, int max) {
            VarName = varName;
            Min = min;
            Max = max;
        }

        internal static RuleCondition ConditionEquals(string varName, int value) {
            return new RuleCondition(varName, value, value);
        }

        internal static RuleCondition ConditionLessThan(string varName, int value) {
            return new RuleCondition(varName, int.MinValue, value);
        }

        internal static RuleCondition ConditionLessThanOrEqual(string varName, int value) {
            return new RuleCondition(varName, int.MinValue, value);
        }

        internal static RuleCondition ConditionGreaterThan(string varName, int value) {
            return new RuleCondition(varName, value, int.MaxValue);
        }

        internal static RuleCondition ConditionGreaterThanOrEqual(string varName, int value) {
            return new RuleCondition(varName, value, int.MaxValue);
        }

        internal bool IsTrue(RuleInvocationContext context) {
            if (!context.TryGetValue(VarName, out int value)) {
                return false;
            }
            if (value == Min && value == Max) {
                return true;
            }
            if (value >= Min && value <= Max) {
                return true;
            }
            return false;
        }

        internal void Serialize(IWriter writer) {
            writer.WriteAsciiPrefix(VarName);
            writer.Write(Min);
            writer.Write(Max);
        }

        internal static RuleCondition Deserialize(IReader reader) {
            string varName = reader.ReadAsciiPrefix();
            int min = reader.ReadInt();
            int max = reader.ReadInt();
            return new RuleCondition(varName, min, max);
        }
    }
}
