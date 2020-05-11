using XPT.Compiling;
using System.Collections.Generic;
using static XPT.Interpreter.Engine;

namespace XPT.Interpreter {
    class EngineFunction : IEngineCallable {
        private readonly Stmt.Function _Declaration;
        private readonly EngineEnvironment _Closure;
        private readonly bool _IsCtor;

        internal EngineFunction(Stmt.Function declaration, EngineEnvironment closure, bool isCtor) {
            _Declaration = declaration;
            _Closure = closure;
            _IsCtor = isCtor;
        }

        public int Arity() => _Declaration.Parameters.Count;

        /// <summary>
        /// Returns a function in a new environment with a reference to 'this' inside the method's original closure.
        /// </summary>
        public EngineFunction Bind(EngineClassInstance instance) {
            EngineEnvironment environment = new EngineEnvironment(_Closure);
            environment.Define(Keywords.This, instance);
            return new EngineFunction(_Declaration, environment, _IsCtor);
        }

        public object Call(Engine interpreter, List<object> arguments) {
            EngineEnvironment environment = new EngineEnvironment(_Closure);
            for (int i = 0; i < _Declaration.Parameters.Count; i++) {
                environment.Define(_Declaration.Parameters[i].Lexeme, arguments[i]);
            }
            try {
                interpreter.ExecuteBlock(_Declaration.Body, environment);
            }
            catch (ReturnException returnValue) {
                if (_IsCtor) {
                    return _Closure.GetAt(0, Keywords.This);
                }
                return returnValue.Value;
            }
            if (_IsCtor) {
                return _Closure.GetAt(0, Keywords.This);
            }
            return null;
        }

        public override string ToString() => $"<fn {_Declaration.Name.Lexeme}>";
    }
}
