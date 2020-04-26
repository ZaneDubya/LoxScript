using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// An implementation of the lox hash table.
    /// </summary>
    class GearsHashTable {
        private Dictionary<string, GearsValue> _Table = new Dictionary<string, GearsValue>();

        public bool Get(string key, out GearsValue value) {
            if (_Table.TryGetValue(key, out value)) {
                return true;
            }
            return false;
        }

        public void Set(string key, GearsValue value) {
            _Table[key] = value;
        }
    }
}
