using LoxScript.Grammar;
using System.Collections.Generic;

namespace LoxScript.Interpreter {
    /// <summary>
    /// A declaration of a class. Behavior lives here.
    /// </summary>
    class EngineClassDeclaration : IEngineCallable {
        internal readonly string Name;

        private readonly EngineClassDeclaration _SuperClass;
        private readonly Dictionary<string, EngineFunction> _Methods;
        private static string CtorName => Keywords.Ctor;

        internal EngineClassDeclaration(string name, EngineClassDeclaration superClass, Dictionary<string, EngineFunction> methods) {
            Name = name;
            _SuperClass = superClass;
            _Methods = methods;
        }

        public int Arity() => FindMethod(CtorName)?.Arity() ?? 0;

        public object Call(Engine interpreter, List<object> arguments) {
            EngineClassInstance instance = new EngineClassInstance(this);
            if (TryFindMethod(CtorName, out EngineFunction ctor)) {
                ctor.Bind(instance).Call(interpreter, arguments);
            }
            return instance;
        }

        public EngineFunction FindMethod(string name) {
            if (_Methods.TryGetValue(name, out EngineFunction method)) {
                return method;
            }
            return _SuperClass?.FindMethod(name) ?? null;
        }

        public bool TryFindMethod(string name, out EngineFunction method) {
            method = FindMethod(name);
            return method != null;
        }

        public override string ToString() => Name;
    }
}
