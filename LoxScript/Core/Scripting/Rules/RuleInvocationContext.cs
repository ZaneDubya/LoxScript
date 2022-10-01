using System.Collections.Generic;

namespace XPT.Core.Scripting.Rules {
    class RuleInvocationContext {
        private readonly Dictionary<ulong, double> _Variables = new Dictionary<ulong, double>();

        internal bool TryGetValue(ulong varNameBitString, out double value) {
            if (_Variables.TryGetValue(varNameBitString, out value)) {
                return true;
            }
            return false;
        }

        internal RuleInvocationContext AddContext(string varName, double value) {
            _Variables[BitString.GetBitStr(varName)] = value;
            return this;
        }
    }
}
