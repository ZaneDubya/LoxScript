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
            ObjString,
        }

        public override string ToString() => "GearsObj";
    }

    class GearsObjString : GearsObj {
        public readonly string Value;

        public GearsObjString(string value) {
            Type = ObjType.ObjString;
            Value = value;
        }

        public override string ToString() => Value;
    }
}
