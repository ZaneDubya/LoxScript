using System.Collections.Generic;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// An implementation of the lox hash table.
    /// </summary>
    class GearsHashTable {
        private Dictionary<string, GearsValue> _Table = new Dictionary<string, GearsValue>();

        public void Reset() {
            _Table.Clear();
        }

        public IEnumerable<GearsValue> All => _Table.Values;

        /*public GearsValue this[string key] {
            get {
                if (_Table.TryGetValue(key, out GearsValue value)) {
                    return value;
                }
                return GearsValue.NilValue;
            }
            set => _Table[key] = value;
        }*/

        /// <summary>
        /// Returns true if value exists in hash table.
        /// </summary>
        public bool TryGet(string key, out GearsValue value) {
            if (_Table.TryGetValue(key, out value)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if value was added to hash table. BUT YOU SHOULDN'T USE THIS, USE CONTAINSKEY.
        /// </summary>
        public bool Set(string key, GearsValue value) {
            bool exists = _Table.ContainsKey(key);
            _Table[key] = value;
            return !exists;
        }

        public void Delete(string key) {
            _Table.Remove(key);
        }

        public bool ContainsKey(string key) => _Table.ContainsKey(key);
    }
}
