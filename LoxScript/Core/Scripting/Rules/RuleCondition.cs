using XPT.Core.Extensions;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class RuleCondition {
        internal readonly long VarNameBitString;
        internal readonly long Min;
        internal readonly long Max;

        private RuleCondition(long varName, long min, long max) {
            VarNameBitString = varName;
            Min = min;
            Max = max;
        }

        internal static RuleCondition ConditionEquals(string varName, long value) {
            long varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, value, value);
        }

        /*internal static RuleCondition ConditionNotEquals(string varName, long value) {
            long varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, value - Epsilon * 2, value + Epsilon * 2);
        }*/

        internal static RuleCondition ConditionLessThan(string varName, long value) {
            long varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, long.MinValue, value);
        }

        internal static RuleCondition ConditionLessThanOrEqual(string varName, long value) {
            long varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, long.MinValue, value);
        }

        internal static RuleCondition ConditionGreaterThan(string varName, long value) {
            long varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, value, long.MaxValue);
        }

        internal static RuleCondition ConditionGreaterThanOrEqual(string varName, long value) {
            long varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, value, long.MaxValue);
        }

        internal bool IsTrue(RuleInvocationContext context) {
            if (!context.TryGetValue(VarNameBitString, out long value)) {
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
            writer.Write(VarNameBitString);
            writer.Write(Min);
            writer.Write(Max);
        }

        internal static RuleCondition Deserialize(IReader reader) {
            long var = reader.ReadLong();
            long min = reader.ReadLong();
            long max = reader.ReadLong();
            return new RuleCondition(var, min, max);
        }
    }
}
