using System.Collections.Generic;

namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// A collection of variables that is passed to RuleSystem when a trigger is invoked. Use Rule.Match() to check
    /// conditions of the rule against the values in the RuleVarCollection to see if the rule has been matched. You 
    /// may add fallback providers with AddVarProvider(); these providers will be checked for variables not found in
    /// the RuleVarCollection. The parent RuleVarCollection variables will take precedence over fallback providers.
    /// </summary>
    internal class RuleVarCollection : IRuleVarProvider {

        private readonly Dictionary<string, object> _Values = new Dictionary<string, object>();
        private readonly List<IRuleVarProvider> _Providers = new List<IRuleVarProvider>();

        internal RuleVarCollection() {

        }

        /// <summary>
        /// Adds a fallback provider to check for variables not found in this collection.
        /// </summary>
        internal void AddVarProvider(IRuleVarProvider provider) {
            if (provider != null && !_Providers.Contains(provider)) {
                _Providers.Add(provider);
            }
        }

        internal void Reset() {
            _Values.Clear();
        }

        internal void ClearRuleVar(string key) {
            key = key.ToLowerInvariant();
            _Values.Remove(key);
        }

        internal void SetRuleVar(string key, object value) {
            key = key.ToLowerInvariant();
            _Values[key] = value;
        }

        public object GetRuleVar(string key) {
            key = key.ToLowerInvariant();
            if (_Values.TryGetValue(key, out object value)) {
                return value;
            }
            foreach (IRuleVarProvider provider in _Providers) {
                if (provider.TryGetRuleVar(key, out value)) {
                    return value;
                }
            }
            return null;
        }

        public bool TryGetRuleVar(string key, out object value) {
            key = key.ToLowerInvariant();
            if (_Values.TryGetValue(key, out value)) {
                return true;
            }
            foreach (IRuleVarProvider provider in _Providers) {
                if (provider.TryGetRuleVar(key, out value)) {
                    return true;
                }
            }
            value = null;
            return false;
        }

        /// <summary>
        /// You can get ints, strings, and objects. Other types should be expected to fail.
        /// </summary>
        public bool TryGetRuleVar<T>(string key, out T value) {
            value = default;
            if (!TryGetRuleVar(key, out object objValue)) {
                return false;
            }
            // Direct type match
            if (objValue is T typedValue) {
                value = typedValue;
                return true;
            }
            // Handle int conversions from other numeric types
            if (typeof(T) == typeof(int)) {
                if (objValue is byte || objValue is sbyte || objValue is short || objValue is ushort || 
                    objValue is int || objValue is uint || objValue is long || objValue is ulong) {
                    value = (T)(object)System.Convert.ToInt32(objValue);
                    return true;
                }
            }
            // Handle string conversions
            if (typeof(T) == typeof(string)) {
                value = (T)(object)objValue.ToString();
                return true;
            }
            return false;
        }
    }
}
