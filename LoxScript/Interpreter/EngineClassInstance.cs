using LoxScript.Scanning;
using System.Collections.Generic;

namespace LoxScript.Interpreter {
    /// <summary>
    /// An instance of a class. Data live here.
    /// </summary>
    internal class EngineClassInstance {
        private readonly EngineClassDeclaration _Class;
        internal readonly Dictionary<string, object> Fields = new Dictionary<string, object>();

        public EngineClassInstance(EngineClassDeclaration loxClass) {
            _Class = loxClass;
        }

        internal object Get(Token name) {
            if (Fields.TryGetValue(name.Lexeme, out object value)) {
                return value;
            }
            if (_Class.TryFindMethod(name.Lexeme, out EngineFunction method)) {
                return method.Bind(this);
            }
            throw new Engine.RuntimeException(name, $"Undefined property '{name.Lexeme}' in {this}.");
        }

        internal void Set(Token name, object value) {
            Fields[name.Lexeme] = value;
        }

        public override string ToString() => $"instance of {_Class}";
    }
}