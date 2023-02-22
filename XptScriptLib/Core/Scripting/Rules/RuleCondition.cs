using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class RuleCondition {

        internal static RuleCondition ConditionEquals(string varName, int value) {
            return new RuleCondition(varName, value, value);
        }

        internal static RuleCondition ConditionEquals(string varName, string value) {
            return new RuleCondition(varName, value);
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

        // === Instance ==============================================================================================
        // ===========================================================================================================

        internal readonly string Key;
        internal readonly int ValueMin;
        internal readonly int ValueMax;
        internal readonly string ValueString;

        private RuleCondition(string keyName, int min, int max) {
            Key = keyName;
            ValueMin = min;
            ValueMax = max;
            ValueString = null;
        }

        private RuleCondition(string keyName, string value) {
            Key = keyName;
            ValueMin = 0;
            ValueMax = 0;
            ValueString = value;
        }

        internal bool IsTrue(ValueCollection context) {
            if (ValueString != null) {
                if (!context.TryGet(Key, out string sValue)) {
                    return false;
                }
                return ValueString == sValue;
            }
            if (!context.TryGet(Key, out int value)) {
                return false;
            }
            if (value == ValueMin && value == ValueMax) {
                return true;
            }
            if (value >= ValueMin && value <= ValueMax) {
                return true;
            }
            return false;
        }

        internal void Serialize(IWriter writer) {
            writer.WriteAsciiPrefix(Key);
            writer.Write(ValueMin);
            writer.Write(ValueMax);
        }

        internal static RuleCondition Deserialize(IReader reader) {
            string varName = reader.ReadAsciiPrefix();
            int min = reader.ReadInt();
            int max = reader.ReadInt();
            return new RuleCondition(varName, min, max);
        }
    }
}
