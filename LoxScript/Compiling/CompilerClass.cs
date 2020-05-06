namespace LoxScript.Compiling {
    class CompilerClass {
        public CompilerClass Enclosing;
        public Token Name;

        public CompilerClass(Token name, CompilerClass enclosing = null) {
            Name = name;
            Enclosing = enclosing;
        }
    }
}
