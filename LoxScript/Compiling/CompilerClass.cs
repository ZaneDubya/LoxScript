namespace LoxScript.Compiling {
    class CompilerClass {
        public CompilerClass Enclosing;
        public Token Name;
        public bool HasSuperClass = false;

        public CompilerClass(Token name, CompilerClass enclosing = null) {
            Name = name;
            Enclosing = enclosing;
        }
    }
}
