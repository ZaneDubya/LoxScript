using System.Collections.Generic;

namespace XPT.Core.Scripting.Rules {
    /// <summary>
    /// A collection of variables that is passed to RuleSystem when a trigger is invoked. RuleSystem checks the 
    /// conditions of each rule against the values in the VarCollection to see if the rule has been matched.
    /// </summary>
    internal class VarCollection {

        private readonly Dictionary<string, object> _Values = new Dictionary<string, object>();

        internal void Reset() {
            _Values.Clear();
        }

        internal void Clear(string key) {
            key = key.ToLowerInvariant();
            _Values.Remove(key);
        }

        internal void Set(string key, object value) {
            key = key.ToLowerInvariant();
            _Values[key] = value;
        }

        internal object Get(string key) {
            key = key.ToLowerInvariant();
            if (_Values.TryGetValue(key, out object value)) {
                return value;
            }
            return null;
        }

        internal T Get<T>(string key) {
            key = key.ToLowerInvariant();
            if (_Values.TryGetValue(key, out object value)) {
                if (value is T typedValue) {
                    return typedValue;
                }
            }
            return default;
        }

        internal bool TryGet<T>(string key, out T value) {
            key = key.ToLowerInvariant();
            if (_Values.TryGetValue(key, out object obj)) {
                if (obj is T typedValue) {
                    value = typedValue;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}
