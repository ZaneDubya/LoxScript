namespace XPT.Compiling {
    struct CompilerFixup {
        public readonly int Address;
        public readonly int Value;

        public CompilerFixup(int address, int value) {
            Address = address;
            Value = value;
        }
    }
}
