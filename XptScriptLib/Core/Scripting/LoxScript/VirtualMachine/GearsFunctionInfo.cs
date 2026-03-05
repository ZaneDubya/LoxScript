using XPT.Core.Scripting.Rules;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// Information about a LoxScript function, including its name and parameter count,.
    /// </summary>
    internal class GearsFunctionInfo {
        /// <summary>
        /// The name of the function as it appears in the global scope.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// The number of parameters the function accepts. Parameter names are not preserved in bytecode.
        /// </summary>
        internal int Arity { get; }

        internal GearsFunctionInfo(string name, int arity) {
            Name = name;
            Arity = arity;
        }

        public override string ToString() {
            return $"{Name}({Arity} param(s))";
        }
    }
}
