using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// An object is a heap-allocated object. It can represent a string, function, class, etc.
    /// </summary>
    class GearsObj {
        public ObjType Type;

        public enum ObjType {
            Free,
            ObjString,
        }
    }

    class GearsObjString : GearsObj {
        public string Value;
    }
}
