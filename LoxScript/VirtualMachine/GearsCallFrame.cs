namespace LoxScript.VirtualMachine {
    class GearsCallFrame {
        public readonly GearsObjFunction Function;
        public int IP;
        public int BP;

        public GearsCallFrame(GearsObjFunction fn, int ip = 0, int bp = 0) {
            Function = fn;
            IP = ip;
            BP = bp;
        }
    }
}
