using XPT.Core.Scripting.Base;

namespace XPT.Core.Scripting.LoxScript.Compiling {
    internal class LoxCompilerClass {
        public LoxCompilerClass Enclosing;
        public Token Name;
        public bool HasSuperClass = false;

        public LoxCompilerClass(Token name, LoxCompilerClass enclosing = null) {
            Name = name;
            Enclosing = enclosing;
        }
    }
}
