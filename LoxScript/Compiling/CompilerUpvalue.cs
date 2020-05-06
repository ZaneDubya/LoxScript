namespace LoxScript.Compiling {
    class CompilerUpvalue {
        public readonly int Index;
        public readonly bool IsLocal;

        public CompilerUpvalue(int index, bool isLocal) {
            Index = index;
            IsLocal = isLocal;
        }

        public override string ToString() => $"{Index}";
    }
}
