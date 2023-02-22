﻿using System.Collections.Generic;

namespace XPT.Core.Scripting.Rules {
    internal class ValueCollection {

        private readonly Dictionary<string, object> _Values = new Dictionary<string, object>();

        internal void Reset() {
            _Values.Clear();
        }

        internal void Clear(string key) {
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