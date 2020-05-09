using System.Collections.Generic;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// An implementation of the lox hash table.
    /// </summary>
    class GearsHashTable {
        private Dictionary<ulong, GearsValue> _Table = new Dictionary<ulong, GearsValue>();

        public void Reset() {
            _Table.Clear();
        }

        public IEnumerable<GearsValue> AllValues => _Table.Values;

        public IEnumerable<ulong> AllKeys => _Table.Keys;

        /// <summary>
        /// Returns true if value exists in hash table.
        /// </summary>
        public bool TryGet(ulong key, out GearsValue value) {
            if (_Table.TryGetValue(key, out value)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if value was added to hash table. BUT YOU SHOULDN'T USE THIS, USE CONTAINSKEY.
        /// </summary>
        public bool Set(ulong key, GearsValue value) {
            bool exists = _Table.ContainsKey(key);
            _Table[key] = value;
            return !exists;
        }

        public void Delete(ulong key) {
            _Table.Remove(key);
        }

        public bool ContainsKey(ulong key) => _Table.ContainsKey(key);
    }
}
