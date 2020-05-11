namespace XPT.VirtualMachine {
    class GearsCallFrame {
        public readonly GearsObjFunction Function;

        public int IP;

        public int BP;

        public GearsCallFrame(GearsObjFunction function, int ip = 0, int bp = 0)  {
            IP = ip;
            BP = bp;
            Function = function;
        }
    }
}
