using LoxScript.Scanning;
using System.Collections.Generic;

namespace LoxScript.Interpreter {
    /// <summary>
    /// A reference to all variables, functions, classes, methods, etc. currently in scope.
    /// </summary>
    class EngineEnvironment {
        internal readonly EngineEnvironment Enclosing;

        private readonly Dictionary<string, object> _Values = new Dictionary<string, object>();

        internal EngineEnvironment(EngineEnvironment enclosing = null) {
            Enclosing = enclosing;
        }

        internal void Define(string name, object value) {
            _Values[name] = value;
        }

        internal void Assign(Token name, object value) {
            if (_Values.ContainsKey(name.Lexeme)) {
                _Values[name.Lexeme] = value;
                return;
            }
            if (Enclosing != null) {
                Enclosing.Assign(name, value);
                return;
            }
            throw new Engine.RuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
        }

        internal object Get(Token name) {
            if (_Values.TryGetValue(name.Lexeme, out object value)) {
                return value;
            }
            if (Enclosing != null) {
                return Enclosing.Get(name);
            }
            throw new Engine.RuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
        }

        internal object GetAt(int distance, string name) {
            // Walks a fixed number of hops up the parent chain and returns the environment there.
            // We know the variable exists in that environment because it has been resolved.
            return Ancestor(distance)._Values[name];
        }

        internal void AssignAt(int distance, Token name, object value) {
            Ancestor(distance)._Values[name.Lexeme] = value;
        }

        private EngineEnvironment Ancestor(int distance) {
            EngineEnvironment environment = this;
            for (int i = 0; i < distance; i++) {
                environment = environment.Enclosing;
            }
            return environment;
        }
    }
}
