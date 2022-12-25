namespace XPT.Core.Scripting.LoxScript.Compiling {
    internal class LoxCompilerUpvalue {
        public readonly int Index;
        public readonly bool IsLocal;

        public LoxCompilerUpvalue(int index, bool isLocal) {
            Index = index;
            IsLocal = isLocal;
        }

        public override string ToString() => $"{Index}";
    }
}
