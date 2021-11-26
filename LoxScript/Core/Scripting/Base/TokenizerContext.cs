namespace XPT.Core.Scripting.Base {
    class TokenizerContext {
        internal string Path;
        internal string Source;
        internal int Start = 0;
        internal int Current = 0;
        internal int Line;

        public TokenizerContext(string path, string source, int line = 1) {
            Path = path;
            Source = source;
            Line = line;
        }
    }
}
