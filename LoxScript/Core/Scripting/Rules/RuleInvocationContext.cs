using System.Collections.Generic;

namespace XPT.Core.Scripting.Rules {
    class RuleInvocationContext {
        private readonly Dictionary<long, long> _Variables = new Dictionary<long, long>();

        internal bool TryGetValue(long varNameBitString, out long value) {
            if (_Variables.TryGetValue(varNameBitString, out value)) {
                return true;
            }
            return false;
        }

        internal RuleInvocationContext AddContext(string varName, long value) {
            _Variables[BitString.GetBitStr(varName)] = value;
            return this;
        }
    }
}
