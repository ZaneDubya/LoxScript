using System.Collections.Generic;

namespace XPT.Interpreter {
    interface IEngineCallable {
        object Call(Engine interpreter, List<object> arguments);

        /// <summary>
        /// The number of expected arguments.
        /// </summary>
        int Arity();
    }
}
