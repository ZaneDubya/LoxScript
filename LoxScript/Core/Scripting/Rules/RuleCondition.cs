using XPT.Core.Extensions;
using XPT.Core.IO;

namespace XPT.Core.Scripting.Rules {
    class RuleCondition {
        private const double Epsilon = 0.0000001f;

        internal readonly ulong VarNameBitString;
        internal readonly double Min;
        internal readonly double Max;

        private RuleCondition(ulong varName, double min, double max) {
            VarNameBitString = varName;
            Min = min;
            Max = max;
        }

        internal static RuleCondition ConditionEquals(string varName, double value) {
            ulong varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, value, value);
        }

        /*internal static RuleCondition ConditionNotEquals(string varName, double value) {
            ulong varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, value - Epsilon * 2, value + Epsilon * 2);
        }*/

        internal static RuleCondition ConditionLessThan(string varName, double value) {
            ulong varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, float.MinValue, value - Epsilon * 2);
        }

        internal static RuleCondition ConditionLessThanOrEqual(string varName, double value) {
            ulong varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, float.MinValue, value);
        }

        internal static RuleCondition ConditionGreaterThan(string varName, double value) {
            ulong varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, value + Epsilon * 2, float.MaxValue);
        }

        internal static RuleCondition ConditionGreaterThanOrEqual(string varName, double value) {
            ulong varname = BitString.GetBitStr(varName);
            return new RuleCondition(varname, value, float.MaxValue);
        }

        internal bool IsTrue(RuleInvocationContext context) {
            if (!context.TryGetValue(VarNameBitString, out double value)) {
                return false;
            }
            if (value == Min && value == Max) {
                return true;
            }
            if (value + Epsilon >= Min && value - Epsilon <= Max) {
                return true;
            }
            return false;
        }

        internal void Serialize(IWriter writer) {
            writer.Write(VarNameBitString);
            writer.Write(Min.ConvertToULong());
            writer.Write(Max.ConvertToULong());
        }

        internal static RuleCondition Deserialize(IReader reader) {
            ulong var = (ulong)reader.ReadLong();
            double min = ((ulong)reader.ReadLong()).ConvertToDouble();
            double max = ((ulong)reader.ReadLong()).ConvertToDouble();
            return new RuleCondition(var, min, max);
        }
    }
}
