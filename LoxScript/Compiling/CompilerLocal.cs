namespace XPT.Compiling {
    class CompilerLocal {
        public readonly string Name;
        public int Depth;
        public bool IsCaptured;

        public CompilerLocal(string name, int depth) {
            Name = name;
            Depth = depth;
            IsCaptured = false;
        }

        public override string ToString() => $"{Name}";
    }
}
