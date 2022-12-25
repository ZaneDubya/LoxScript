using System.Collections.Generic;

namespace XPT.Core.Scripting.Rules {
    class RuleInvocationContext {
        private readonly Dictionary<string, int> _Variables = new Dictionary<string, int>();

        internal bool TryGetValue(string varNameBitString, out int value) {
            if (_Variables.TryGetValue(varNameBitString, out value)) {
                return true;
            }
            return false;
        }

        internal RuleInvocationContext AddContext(string varName, int value) {
            _Variables[varName] = value;
            return this;
        }
    }
}
