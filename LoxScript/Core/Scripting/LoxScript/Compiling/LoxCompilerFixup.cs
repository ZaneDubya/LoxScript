namespace XPT.Core.Scripting.LoxScript.Compiling {
    internal struct LoxCompilerFixup {
        public readonly int Address;
        public readonly int Value;

        public LoxCompilerFixup(int address, int value) {
            Address = address;
            Value = value;
        }
    }
}
