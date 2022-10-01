#if NET_4_5
using System.Runtime.CompilerServices;
#endif

using System.Collections.Generic;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// An implementation of the lox hash table.
    /// </summary>
    internal class GearsHashTable {
        private Dictionary<ulong, GearsValue> _Table = new Dictionary<ulong, GearsValue>();

        public void Reset() {
            _Table.Clear();
        }

        public IEnumerable<GearsValue> AllValues => _Table.Values;

        public IEnumerable<ulong> AllKeys => _Table.Keys;

        /// <summary>
        /// Returns true if value exists in hash table.
        /// </summary>
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool TryGet(ulong key, out GearsValue value) {
            if (_Table.TryGetValue(key, out value)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if value replaced an existing value.
        /// </summary>
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool Set(ulong key, GearsValue value) {
            bool exists = _Table.ContainsKey(key);
            _Table[key] = value;
            return !exists;
        }

        public void Delete(ulong key) {
            _Table.Remove(key);
        }

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool ContainsKey(ulong key) => _Table.ContainsKey(key);
    }
}
