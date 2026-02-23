using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// When a Rule trigger is matched, all the RuleConditions for that rule are checked, to see if the rule is true.
    /// An event is fired if one (or more) rules are true. An event will only be fired once if multiple rules are true.
    /// </summary>
    class RuleCondition {

        internal static RuleCondition ConditionEquals(string varName, int value) {
            return new RuleCondition(varName, value, value, false);
        }

        internal static RuleCondition ConditionEquals(string varName, string value) {
            return new RuleCondition(varName, value, false);
        }

        internal static RuleCondition ConditionNotEquals(string varName, int value) {
            return new RuleCondition(varName, value, value, true);
        }

        internal static RuleCondition ConditionNotEquals(string varName, string value) {
            return new RuleCondition(varName, value, true);
        }

        internal static RuleCondition ConditionLessThan(string varName, int value) {
            return new RuleCondition(varName, int.MinValue, value - 1, false);
        }

        internal static RuleCondition ConditionLessThanOrEqual(string varName, int value) {
            return new RuleCondition(varName, int.MinValue, value, false);
        }

        internal static RuleCondition ConditionGreaterThan(string varName, int value) {
            return new RuleCondition(varName, value + 1, int.MaxValue, false);
        }

        internal static RuleCondition ConditionGreaterThanOrEqual(string varName, int value) {
            return new RuleCondition(varName, value, int.MaxValue, false);
        }

        // === Instance ==============================================================================================
        // ===========================================================================================================

        internal readonly string Key;
        internal readonly bool IsNegated;
        internal readonly int ValueMin;
        internal readonly int ValueMax;
        internal readonly string ValueString;

        private RuleCondition(string keyName, int min, int max, bool isNegated) {
            Key = keyName;
            ValueMin = min;
            ValueMax = max;
            ValueString = null;
            IsNegated = isNegated;
        }

        private RuleCondition(string keyName, string value, bool isNegated) {
            Key = keyName;
            ValueMin = 0;
            ValueMax = 0;
            ValueString = value;
            IsNegated = isNegated;
        }

        internal bool IsTrue(VarCollection context) {
            if (ValueString != null) {
                if (!context.TryGet(Key, out string sValue)) {
                    return false;
                }
                bool matches = ValueString == sValue;
                return IsNegated ? !matches : matches;
            }
            if (!context.TryGet(Key, out int value)) {
                return false;
            }
            bool inRange;
            if (value == ValueMin && value == ValueMax) {
                inRange = true;
            }
            else if (value >= ValueMin && value <= ValueMax) {
                inRange = true;
            }
            else {
                inRange = false;
            }
            return IsNegated ? !inRange : inRange;
        }

        internal void Serialize(IWriter writer) {
            writer.WriteAsciiPrefix(Key);
            writer.Write(IsNegated);
            if (ValueString != null) {
                writer.Write(true);
                writer.WriteAsciiPrefix(ValueString);
            }
            else {
                writer.Write(false);
                writer.Write(ValueMin);
                writer.Write(ValueMax);
            }
        }

        internal static RuleCondition Deserialize(IReader reader) {
            string varName = reader.ReadAsciiPrefix();
            bool isNegated = reader.ReadBool();
            if (reader.ReadBool()) {
                string valueString = reader.ReadAsciiPrefix();
                return new RuleCondition(varName, valueString, isNegated);
            }
            else {
                int min = reader.ReadInt();
                int max = reader.ReadInt();
                return new RuleCondition(varName, min, max, isNegated);
            }
        }
    }
}
