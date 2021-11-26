namespace XPT.Core.Scripting.Base {
    abstract class ACompiler {

        // input and error:
        protected readonly TokenList Tokens;
        protected bool HadError = false;

        // Compiling code to:
        protected readonly string Name;

        protected ACompiler(TokenList tokens, string name) {
            Tokens = tokens;
            Name = name;
        }

        /// <summary>
        /// program     → declaration* EOF ;
        /// Called from TryCompile.
        /// </summary>
        internal bool Compile() {
            while (!Tokens.IsAtEnd()) {
                try {
                    Declaration();
                }
                catch {
                    HadError = true;
                    Synchronize();
                    throw;
                }
            }
            EndCompiler();
            return !HadError;
        }

        protected abstract void Declaration();

        protected abstract void Synchronize();

        protected abstract void EndCompiler();

        protected int LineOfLastToken => Tokens.Previous().Line;

        protected int LineOfCurrentToken => Tokens.Peek().Line;

        public override string ToString() => $"Compiling {Name}";
    }
}
