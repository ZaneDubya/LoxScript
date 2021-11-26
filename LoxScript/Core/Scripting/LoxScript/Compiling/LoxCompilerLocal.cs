namespace XPT.Core.Scripting.LoxScript.Compiling {
    internal class LoxCompilerLocal {
        public readonly string Name;
        public int Depth;
        public bool IsCaptured;

        public LoxCompilerLocal(string name, int depth) {
            Name = name;
            Depth = depth;
            IsCaptured = false;
        }

        public override string ToString() => $"{Name}";
    }
}
