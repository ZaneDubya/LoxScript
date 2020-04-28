using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxScript.VirtualMachine {
    class GearsCallFrame {
        public readonly GearsObjFunction Function;
        public readonly int IP;
        public GearsValue[] Slots;
    }
}
